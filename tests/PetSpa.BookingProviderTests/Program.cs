using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Npgsql;
using PetSpa.Api.Authentication;
using PetSpa.Api.Composition;
using PetSpa.Modules.Booking;
using PetSpa.Modules.Booking.Contracts;
using PetSpa.Modules.Booking.Domain.Enums;
using PetSpa.Modules.Booking.Infrastructure.Persistence;
using PetSpa.Modules.Customer;
using PetSpa.Modules.Customer.Endpoints;
using PetSpa.Modules.Resource;
using PetSpa.Modules.Resource.Contracts;
using PetSpa.Modules.Resource.Infrastructure.Persistence;
using PetSpa.SharedKernel.Application.Abstractions;
using PetSpa.SharedKernel.Application.Authentication;

var baseConnection = Environment.GetEnvironmentVariable("PETSPA_BOOKING_TEST_CONNECTION");
if (string.IsNullOrWhiteSpace(baseConnection))
{
    Console.Error.WriteLine("Set PETSPA_BOOKING_TEST_CONNECTION to a disposable PostgreSQL test database.");
    return 2;
}
var httpConnection = new NpgsqlConnectionStringBuilder(baseConnection) { ApplicationName = "BookingProviderQaHttp" };
var callerConnection = new NpgsqlConnectionStringBuilder(baseConnection) { ApplicationName = "BookingProviderQaCaller" };
var builder = WebApplication.CreateSlimBuilder(args);
builder.Logging.ClearProviders();
builder.WebHost.UseUrls("http://127.0.0.1:0");
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddCustomerModule(httpConnection.ConnectionString);
builder.Services.AddResourceModule(httpConnection.ConnectionString);
builder.Services.AddBookingModule(httpConnection.ConnectionString);
builder.Services.AddScoped(typeof(IPasswordHasher<>), typeof(PasswordHasher<>));
builder.Services.AddScoped(typeof(IPasswordService<>), typeof(IdentityPasswordService<>));
builder.Services.AddAuthentication(QaAuthentication.SchemeName)
    .AddScheme<AuthenticationSchemeOptions, QaAuthentication>(QaAuthentication.SchemeName, _ => { });
builder.Services.AddAuthorizationServices();
await using var app = builder.Build();
app.UseExceptionHandler(error => error.Run(context =>
{
    var exception = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>()?.Error;
    Console.Error.WriteLine(exception);
    return context.Response.WriteAsJsonAsync(new { error = exception?.Message });
}));
app.UseAuthentication();
app.UseAuthorization();
app.MapCustomerEndpoint();
app.MapPetEndpoint();
app.MapResourceModuleEndpoint();
app.MapBookingModuleEndpoint();

await using (var scope = app.Services.CreateAsyncScope())
{
    var customerDbContextType = typeof(CustomerModule).Assembly.GetType(
        "PetSpa.Modules.Customer.Infrastructure.Persistence.CustomerDbContext", throwOnError: true)!;
    var customerDb = (DbContext)scope.ServiceProvider.GetRequiredService(customerDbContextType);
    await customerDb.Database.MigrateAsync();
    await scope.ServiceProvider.GetRequiredService<ResourceDbContext>().Database.MigrateAsync();
    await scope.ServiceProvider.GetRequiredService<BookingDbContext>().Database.MigrateAsync();
}

await using var observer = new NpgsqlConnection(callerConnection.ConnectionString);
await observer.OpenAsync();
await using (var createMockConsumerTables = new NpgsqlCommand("""
    CREATE SCHEMA IF NOT EXISTS provider_qa;
    CREATE TABLE IF NOT EXISTS provider_qa.sessions (
        booking_id bigint NOT NULL,
        detail_id bigint NOT NULL,
        PRIMARY KEY (booking_id, detail_id)
    );
    CREATE TABLE IF NOT EXISTS provider_qa.payments (booking_id bigint PRIMARY KEY);
    """, observer))
    await createMockConsumerTables.ExecuteNonQueryAsync();

await app.StartAsync();
var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
using var client = new HttpClient { BaseAddress = new Uri(address), Timeout = TimeSpan.FromSeconds(12) };
using var providerScope = app.Services.CreateScope();
var provider = providerScope.ServiceProvider.GetRequiredService<IBookingModule>();
var resource = providerScope.ServiceProvider.GetRequiredService<IResourceModule>();
var marker = Guid.NewGuid().ToString("N");
var appointment = DateTime.UtcNow.Date.AddDays(3).AddHours(13);
var assertions = 0;
const decimal firstPrice = 115_000;
const decimal changedPrice = 175_000;
const decimal secondPrice = 80_000;

void Require(bool condition, string message)
{
    if (!condition)
        throw new InvalidOperationException(message);
    Interlocked.Increment(ref assertions);
}

async Task<(HttpStatusCode Status, JsonElement Json, string Body)> Send(
    HttpMethod method, string path, object? body = null, string? role = AccountRoles.Receptionist, long accountId = 1)
{
    using var request = new HttpRequestMessage(method, path);
    if (role is not null)
    {
        request.Headers.Add(QaAuthentication.RoleHeader, role);
        request.Headers.Add(QaAuthentication.IdHeader, accountId.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }
    if (body is not null)
        request.Content = JsonContent.Create(body);
    using var response = await client.SendAsync(request);
    var raw = await response.Content.ReadAsStringAsync();
    if (string.IsNullOrWhiteSpace(raw))
        return (response.StatusCode, default, raw);
    using var doc = JsonDocument.Parse(raw);
    return (response.StatusCode, doc.RootElement.Clone(), raw);
}

async Task<JsonElement> Request(HttpStatusCode expected, HttpMethod method, string path, object? body = null,
    string? role = AccountRoles.Receptionist, long accountId = 1)
{
    var response = await Send(method, path, body, role, accountId);
    Require(response.Status == expected, $"{method} {path}: expected {expected}, got {response.Status}. {response.Body}");
    return response.Json;
}

Task<JsonElement> Get(string path) => Request(HttpStatusCode.OK, HttpMethod.Get, path);
Task<JsonElement> Manager(HttpStatusCode expected, HttpMethod method, string path, object? body = null)
    => Request(expected, method, path, body, AccountRoles.Manager);
Task<JsonElement> ChangeStatus(HttpStatusCode expected, long id, string status)
    => Request(expected, HttpMethod.Patch, $"/api/booking/{id}", new { status });
Task<JsonElement> ReadBooking(long id) => Get($"/api/bookings/{id}");
static long Id(JsonElement row) => row.GetProperty("id").GetInt64();
static string? Value(JsonElement row, string key) => row.GetProperty(key).GetString();
static JsonElement[] Services(JsonElement row) => row.GetProperty("services").EnumerateArray().ToArray();
static JsonElement Service(JsonElement row, long option) =>
    Services(row).Single(detail => detail.GetProperty("serviceOptionId").GetInt64() == option);
static object Option(long serviceId, decimal unitPrice, int durationMinutes) =>
    new { serviceId, unitPrice, species = "DOG", minWeight = 1, maxWeight = 10, durationMinutes };
T Success<T>(BookingContractResult<T> result)
{
    Require(result.Status == BookingContractStatus.Success && result.Value is not null,
        $"Expected success from {typeof(T).Name}, got {result.Status}. {result.Message}");
    return result.Value!;
}

async Task<NpgsqlConnection> OpenCaller()
{
    var connection = new NpgsqlConnection(callerConnection.ConnectionString);
    await connection.OpenAsync();
    return connection;
}

static NpgsqlCommand BookingCommand(NpgsqlConnection connection, string sql, long id, NpgsqlTransaction? transaction = null)
{
    var command = new NpgsqlCommand(sql, connection, transaction);
    command.Parameters.AddWithValue("id", id);
    return command;
}

static async Task<int> CountConsumerRows(
    NpgsqlConnection connection, string table, long bookingId, NpgsqlTransaction? transaction = null)
{
    await using var command = BookingCommand(connection,
        $"SELECT count(*)::integer FROM provider_qa.{table} WHERE booking_id = @id", bookingId, transaction);
    return (int)(await command.ExecuteScalarAsync())!;
}

async Task FailConsumerWriteAndRollback(NpgsqlCommand duplicateInsert, NpgsqlTransaction transaction)
{
    PostgresException? failure = null;
    try
    {
        await duplicateInsert.ExecuteNonQueryAsync();
    }
    catch (PostgresException exception)
    {
        failure = exception;
    }
    Require(failure?.SqlState == PostgresErrorCodes.UniqueViolation, "Expected the consumer's duplicate write to fail.");
    await transaction.RollbackAsync();
}

async Task WaitForBookingLock(Task request)
{
    var deadline = Stopwatch.StartNew();
    while (deadline.Elapsed < TimeSpan.FromSeconds(6) && !request.IsCompleted)
    {
        await using var query = new NpgsqlCommand("""
            SELECT EXISTS (
                SELECT FROM pg_stat_activity
                WHERE application_name = 'BookingProviderQaHttp'
                AND wait_event_type = 'Lock'
                AND query ILIKE '%booking.booking%FOR UPDATE%'
            )
            """, observer);
        if ((bool)(await query.ExecuteScalarAsync())!)
        {
            Require(!request.IsCompleted, "Expected HTTP mutation waiting on the PostgreSQL row lock.");
            return;
        }
        await Task.Delay(40);
    }
    throw new InvalidOperationException("Expected a PostgreSQL booking row lock held across the provider transaction.");
}

async Task InsertSessions(long bookingId, IReadOnlyCollection<long> detailIds,
    NpgsqlConnection connection, NpgsqlTransaction transaction)
{
    foreach (var id in detailIds)
    {
        await using var command = BookingCommand(connection,
            "INSERT INTO provider_qa.sessions (booking_id, detail_id) VALUES (@id, @detail)",
            bookingId, transaction);
        command.Parameters.AddWithValue("detail", id);
        Require(await command.ExecuteNonQueryAsync() == 1, "Expected one mock Operation session row.");
    }
}

var fullName = "Booking QA " + marker;
var originalPhone = "09" + Random.Shared.Next(10_000_000, 99_999_999).ToString();
const string separatePhone = "0987654321";
var customer = Id(await Request(HttpStatusCode.Created, HttpMethod.Post, "/api/customers",
    new { fullName, phone = originalPhone, password = "PetSpa Qa password 2026#" }));
var pet = Id(await Request(HttpStatusCode.Created, HttpMethod.Post, $"/api/customers/{customer}/pets",
    new { customersId = customer, name = "Pixel", species = "DOG", weight = 4, height = 25 }));
var category = Id(await Manager(HttpStatusCode.Created, HttpMethod.Post, "/api/categories", new { name = marker }));
var service = Id(await Manager(HttpStatusCode.Created, HttpMethod.Post, "/api/services",
    new { categoryId = category, serviceName = "Bath " + marker, description = "Bath services" }));
var firstOption = Id(await Manager(HttpStatusCode.Created, HttpMethod.Post, "/api/service-options", Option(service, firstPrice, 30)));
var secondOption = Id(await Manager(HttpStatusCode.Created, HttpMethod.Post, "/api/service-options", Option(service, secondPrice, 55)));

object BookingBody(long[] serviceOptionIds, DateTime? bookingTime = null) =>
    new { customerId = customer, petId = pet, bookingTime = bookingTime ?? appointment, serviceOptionIds, note = "Counter QA" };
Task<JsonElement> Create(long[] serviceOptionIds, DateTime? bookingTime = null) =>
    Request(HttpStatusCode.Created, HttpMethod.Post, "/api/bookings", BookingBody(serviceOptionIds, bookingTime));

await Request(HttpStatusCode.Unauthorized, HttpMethod.Post, "/api/bookings", BookingBody([firstOption]), role: null);
foreach (var role in new[] { AccountRoles.Customer, AccountRoles.Staff, AccountRoles.Admin })
    await Request(HttpStatusCode.Forbidden, HttpMethod.Post, "/api/bookings", BookingBody([firstOption]), role);
await Request(HttpStatusCode.BadRequest, HttpMethod.Post, "/api/bookings", BookingBody([firstOption, firstOption]));
await Request(HttpStatusCode.BadRequest, HttpMethod.Post, "/api/bookings", BookingBody([]));

var main = await Create([firstOption, secondOption]);
var mainId = Id(main);
var firstDetailId = Id(Service(main, firstOption));
var secondDetailId = Id(Service(main, secondOption));
Require(Value(main, "status") == "PENDING" && Value(main, "contactName") == fullName
    && Value(main, "contactPhone") == originalPhone, "Create should snapshot active Customer contact and use PENDING.");
Require(Services(main).All(detail => Value(detail, "status") == "ASSIGNED"), "Expected an ASSIGNED set.");
await Request(HttpStatusCode.OK, HttpMethod.Get, $"/api/bookings/{mainId}", role: AccountRoles.Customer, accountId: customer);
await Request(HttpStatusCode.Forbidden, HttpMethod.Get, $"/api/bookings/{mainId}", role: AccountRoles.Customer, accountId: customer + 1);
await Manager(HttpStatusCode.OK, HttpMethod.Put, $"/api/service-options/{firstOption}", Option(service, changedPrice, 30));
Require(Service(await ReadBooking(mainId), firstOption).GetProperty("unitPrice").GetDecimal() == firstPrice,
    "Resource price edit changed the existing booking snapshot.");
var replacement = await Request(HttpStatusCode.OK, HttpMethod.Put, $"/api/bookings/{mainId}/services",
    new { serviceOptionIds = new[] { secondOption, firstOption } });
Require(Id(Service(replacement, firstOption)) == firstDetailId && Id(Service(replacement, secondOption)) == secondDetailId
    && Service(replacement, firstOption).GetProperty("unitPrice").GetDecimal() == firstPrice,
    "Retained details must preserve their ID and snapped price.");

var replacementFirst = await Create([secondOption], appointment.AddDays(1));
var replacementId = Id(replacementFirst);
var added = await Request(HttpStatusCode.OK, HttpMethod.Put, $"/api/bookings/{replacementId}/services",
    new { serviceOptionIds = new[] { secondOption, firstOption } });
Require(Id(Service(added, secondOption)) == Id(Service(replacementFirst, secondOption))
    && Service(added, firstOption).GetProperty("unitPrice").GetDecimal() == changedPrice,
    "Newly added details must use the current price and retain the original row.");
await ChangeStatus(HttpStatusCode.OK, replacementId, "confirmed");
var reduced = await Request(HttpStatusCode.OK, HttpMethod.Put, $"/api/bookings/{replacementId}/services",
    new { serviceOptionIds = new[] { secondOption } });
Require(Services(reduced).Select(Id).SequenceEqual([Id(Service(replacementFirst, secondOption))]),
    "Confirmed booking must permit replacement before handoff.");
await using (var connection = await OpenCaller())
await using (var transaction = await connection.BeginTransactionAsync())
{
    var staleIds = Services(added).Select(Id).ToArray();
    Require((await provider.MarkSessionsCreatedAsync(replacementId, staleIds, transaction)).Status == BookingContractStatus.Conflict,
        "Handoff must refuse the stale set when replacement wins.");
    await transaction.RollbackAsync();
}

await Request(HttpStatusCode.BadRequest, HttpMethod.Put, $"/api/bookings/{mainId}", new { customerId = customer, note = "Rejected" });
Require(Value(await ReadBooking(mainId), "note") == "Counter QA", "Unexpected fields must reject an update atomically.");
var edited = await Request(HttpStatusCode.OK, HttpMethod.Put, $"/api/bookings/{mainId}",
    new { bookingTime = appointment, contactName = "Booking contact", contactPhone = separatePhone, note = "Updated at front desk" });
Require(Value(edited, "contactName") == "Booking contact" && Value(edited, "contactPhone") == separatePhone
    && Value(await Get($"/api/customers/{customer}"), "phone") == originalPhone, "Booking contact updates must stay local.");

await Request(HttpStatusCode.BadRequest, HttpMethod.Patch, $"/api/booking/{mainId}", new { status = "confirmed", note = "Rejected" });
await ChangeStatus(HttpStatusCode.BadRequest, mainId, "paid");
var checks = await Task.WhenAll(ChangeStatus(HttpStatusCode.OK, mainId, "confirmed"), ChangeStatus(HttpStatusCode.OK, mainId, "confirmed"));
var confirmed = checks[0];
var checkIn = Value(confirmed, "checkIn");
Require(checkIn is not null && checks.All(check => Value(check, "checkIn") == checkIn && Value(check, "status") == "CONFIRMED"),
    "Repeated concurrent confirmation must preserve checkIn.");
await Request(HttpStatusCode.Conflict, HttpMethod.Put, $"/api/bookings/{mainId}", new { note = "Late update" });
await ChangeStatus(HttpStatusCode.Conflict, mainId, "completed");
Require((await provider.GetBillableBookingAsync(mainId)).Status == BookingContractStatus.Conflict,
    "Billing should reject unfinalized bookings.");
var expectedDetails = new[] { firstDetailId, secondDetailId }.Order().ToArray();

await using (var connection = await OpenCaller())
await using (var transaction = await connection.BeginTransactionAsync())
{
    var candidate = Success(await provider.GetHandoffCandidateAsync(mainId, transaction));
    Require(candidate.PetId == pet && candidate.Details.Select(detail => detail.BookingDetailId).SequenceEqual(expectedDetails),
        "Handoff candidate must expose the exact active IDs.");
    Require((await provider.MarkSessionsCreatedAsync(mainId, [firstDetailId], transaction)).Status == BookingContractStatus.Conflict,
        "Handoff must reject an incomplete set.");
    Require((await provider.MarkSessionsCreatedAsync(mainId, [firstDetailId, firstDetailId], transaction)).Status == BookingContractStatus.Conflict,
        "Handoff must reject duplicated IDs.");
    await InsertSessions(mainId, expectedDetails, connection, transaction);
    var mark = Success(await provider.MarkSessionsCreatedAsync(mainId, expectedDetails, transaction));
    Require(mark.BookingDetailIds.SequenceEqual(expectedDetails), "Provider must save exact detail IDs.");
    await using var readMarker = BookingCommand(connection,
        "SELECT sessions_created_at IS NOT NULL FROM booking.booking WHERE id = @id", mainId, transaction);
    Require((bool)(await readMarker.ExecuteScalarAsync())! && await CountConsumerRows(connection, "sessions", mainId, transaction) == 2,
        "Booking and mock session writes must be visible inside the caller transaction.");
    await using var duplicateSession = BookingCommand(connection,
        "INSERT INTO provider_qa.sessions (booking_id, detail_id) VALUES (@id, @detail)", mainId, transaction);
    duplicateSession.Parameters.AddWithValue("detail", firstDetailId);
    await FailConsumerWriteAndRollback(duplicateSession, transaction);
}
Require(Value(await ReadBooking(mainId), "sessionsCreatedAt") is null
    && await CountConsumerRows(observer, "sessions", mainId) == 0, "Rollback did not undo Booking and Operation together.");

DateTime handoffAt;
await using (var connection = await OpenCaller())
await using (var transaction = await connection.BeginTransactionAsync())
{
    Success(await provider.GetHandoffCandidateAsync(mainId, transaction));
    await InsertSessions(mainId, expectedDetails, connection, transaction);
    handoffAt = Success(await provider.MarkSessionsCreatedAsync(mainId, expectedDetails, transaction)).SessionsCreatedAt;
    var replace = Send(HttpMethod.Put, $"/api/bookings/{mainId}/services", new { serviceOptionIds = new[] { firstOption } });
    await WaitForBookingLock(replace);
    await transaction.CommitAsync();
    var replaced = await replace;
    Require(replaced.Status == HttpStatusCode.Conflict, $"Replace lost no race and should return 409. Got {replaced.Status}. {replaced.Body}");
}
Require(await CountConsumerRows(observer, "sessions", mainId) == 2, "Handoff must commit both mocked Operation rows.");
await using (var connection = await OpenCaller())
await using (var transaction = await connection.BeginTransactionAsync())
{
    var repeated = Success(await provider.MarkSessionsCreatedAsync(mainId, expectedDetails.Reverse().ToArray(), transaction));
    Require(repeated.SessionsCreatedAt == handoffAt && repeated.BookingDetailIds.SequenceEqual(expectedDetails),
        "Retry must preserve the timestamp and set irrespective of order.");
    Require((await provider.MarkSessionsCreatedAsync(mainId, [firstDetailId, long.MaxValue], transaction)).Status == BookingContractStatus.Conflict,
        "Handoff must reject a changed set.");
    Require((await provider.SyncDetailStatusAsync(mainId, long.MaxValue, BookingDetailStatus.COMPLETED, transaction)).Status == BookingContractStatus.Conflict,
        "Provider must reject IDs outside the handoff set.");
    Success(await provider.SyncDetailStatusAsync(mainId, firstDetailId, BookingDetailStatus.WORKING, transaction));
    Success(await provider.SyncDetailStatusAsync(mainId, firstDetailId, BookingDetailStatus.COMPLETED, transaction));
    await transaction.CommitAsync();
}
Require(Value(await ReadBooking(mainId), "status") == "CONFIRMED", "Finishing a detail must leave Booking CONFIRMED.");
await ChangeStatus(HttpStatusCode.Conflict, mainId, "completed");
await Request(HttpStatusCode.Conflict, HttpMethod.Delete, $"/api/bookings/{mainId}");

JsonElement completed;
await using (var connection = await OpenCaller())
await using (var transaction = await connection.BeginTransactionAsync())
{
    Success(await provider.SyncDetailStatusAsync(mainId, secondDetailId, BookingDetailStatus.CANCELLED, transaction));
    var complete = Send(HttpMethod.Patch, $"/api/booking/{mainId}", new { status = "completed" });
    await WaitForBookingLock(complete);
    await transaction.CommitAsync();
    var result = await complete;
    Require(result.Status == HttpStatusCode.OK, $"Finalize must see the committed last detail. Got {result.Status}. {result.Body}");
    completed = result.Json;
}
var completionRetry = await ChangeStatus(HttpStatusCode.OK, mainId, "completed");
Require(Value(completed, "status") == "COMPLETED" && completionRetry.GetRawText() == completed.GetRawText(),
    "Repeated completion must return the previous response without changing its timestamps.");
var billable = Success(await provider.GetBillableBookingAsync(mainId));
Require(billable.Details.Count == 1 && billable.Details.Single() ==
    new BillableBookingDetail(firstDetailId, firstOption, firstPrice), "Billing must see only the completed detail at the original price.");
await Manager(HttpStatusCode.NoContent, HttpMethod.Delete, $"/api/service-options/{firstOption}");
Require((await resource.GetServiceOptionDescriptionsAsync([firstOption], default)).Single().Description == "Bath services",
    "Billing must resolve the historical option after deactivation.");

await using (var connection = await OpenCaller())
await using (var transaction = await connection.BeginTransactionAsync())
{
    await using var payment = BookingCommand(connection,
        "INSERT INTO provider_qa.payments (booking_id) VALUES (@id)", mainId, transaction);
    Require(await payment.ExecuteNonQueryAsync() == 1, "Expected a mocked payment.");
    Require(Success(await provider.MarkPaidAsync(mainId, transaction)).Status == BookingStatus.PAID, "Booking must transition to PAID.");
    await FailConsumerWriteAndRollback(payment, transaction);
}
Require(Value(await ReadBooking(mainId), "status") == "COMPLETED"
    && await CountConsumerRows(observer, "payments", mainId) == 0, "Payment rollback must restore COMPLETED.");
await using (var connection = await OpenCaller())
await using (var transaction = await connection.BeginTransactionAsync())
{
    await using var payment = BookingCommand(connection,
        "INSERT INTO provider_qa.payments (booking_id) VALUES (@id)", mainId, transaction);
    Require(await payment.ExecuteNonQueryAsync() == 1, "Expected a mocked payment.");
    Success(await provider.MarkPaidAsync(mainId, transaction));
    Require(Success(await provider.MarkPaidAsync(mainId, transaction)).Status == BookingStatus.PAID, "Repeated PAID must succeed.");
    await transaction.CommitAsync();
}
Require(Value(await ReadBooking(mainId), "status") == "PAID"
    && await CountConsumerRows(observer, "payments", mainId) == 1, "Payment and PAID must commit together.");

async Task HandoffAndSync(long booking, BookingDetailStatus status)
{
    await using var connection = await OpenCaller();
    await using var transaction = await connection.BeginTransactionAsync();
    var handoff = Success(await provider.GetHandoffCandidateAsync(booking, transaction));
    var ids = handoff.Details.Select(detail => detail.BookingDetailId).ToArray();
    await InsertSessions(booking, ids, connection, transaction);
    Success(await provider.MarkSessionsCreatedAsync(booking, ids, transaction));
    foreach (var id in ids)
        Success(await provider.SyncDetailStatusAsync(booking, id, status, transaction));
    await transaction.CommitAsync();
}

var allCancelled = Id(await Create([secondOption]));
await ChangeStatus(HttpStatusCode.OK, allCancelled, "confirmed");
await HandoffAndSync(allCancelled, BookingDetailStatus.CANCELLED);
await ChangeStatus(HttpStatusCode.Conflict, allCancelled, "completed");
await Request(HttpStatusCode.NoContent, HttpMethod.Delete, $"/api/bookings/{allCancelled}");
var cancelAt = Value(await ReadBooking(allCancelled), "cancelledAt");
await Request(HttpStatusCode.NoContent, HttpMethod.Delete, $"/api/bookings/{allCancelled}");
var cancelled = await ReadBooking(allCancelled);
Require(Value(cancelled, "status") == "CANCELLED" && cancelAt is not null && Value(cancelled, "cancelledAt") == cancelAt,
    "Repeated cancellation must preserve cancelledAt.");

var cancelledEarly = Id(await Create([secondOption]));
await Request(HttpStatusCode.NoContent, HttpMethod.Delete, $"/api/bookings/{cancelledEarly}");
Require(Services(await ReadBooking(cancelledEarly)).All(detail => Value(detail, "status") == "CANCELLED"),
    "Cancellation before handoff must cancel details.");

var completedForList = Id(await Create([secondOption]));
await ChangeStatus(HttpStatusCode.OK, completedForList, "confirmed");
await HandoffAndSync(completedForList, BookingDetailStatus.COMPLETED);
await ChangeStatus(HttpStatusCode.OK, completedForList, "completed");
var confirmedForList = Id(await Create([secondOption]));
await ChangeStatus(HttpStatusCode.OK, confirmedForList, "confirmed");
var pendingOne = Id(await Create([secondOption]));
var pendingTwo = Id(await Create([secondOption]));
var pendingThree = Id(await Create([secondOption]));
var list = $"/api/bookings?date={appointment:yyyy-MM-dd}&customerId={customer}";
var expectedOrder = new[] { completedForList, confirmedForList, mainId, pendingOne, pendingTwo, pendingThree, allCancelled, cancelledEarly };
var actualOrder = new List<long>();
string? cursor = null;
var pageCount = 0;
do
{
    var page = await Get($"{list}&pageSize=2" + (cursor is null ? "" : "&cursor=" + Uri.EscapeDataString(cursor)));
    actualOrder.AddRange(page.GetProperty("items").EnumerateArray().Select(Id));
    cursor = Value(page, "nextCursor");
    pageCount++;
} while (cursor is not null && pageCount <= expectedOrder.Length);
Require(cursor is null && actualOrder.SequenceEqual(expectedOrder),
    $"Pagination order or tied cursor incorrect. Expected {string.Join(", ", expectedOrder)}, got {string.Join(", ", actualOrder)}.");
var byContact = await Get($"{list}&contactPhone={separatePhone}&status=paid");
Require(byContact.GetProperty("items").EnumerateArray().Select(Id).SequenceEqual([mainId]), "Stored contact/status filter failed.");
var empty = await Get($"{list}&contactPhone=0900000000");
Require(empty.GetProperty("items").GetArrayLength() == 0 && Value(empty, "nextCursor") is null, "Empty page must be 200 with empty items.");
await Request(HttpStatusCode.BadRequest, HttpMethod.Get, $"{list}&cursor=invalid");

await Request(HttpStatusCode.BadRequest, HttpMethod.Post, "/api/bookings", BookingBody([firstOption]));
await Manager(HttpStatusCode.Created, HttpMethod.Put, $"/api/pets/{pet}", new { status = "INACTIVE" });
await Request(HttpStatusCode.BadRequest, HttpMethod.Post, "/api/bookings", BookingBody([secondOption]));
Require(Value(await ReadBooking(mainId), "status") == "PAID", "Inactive pet must not hide historical bookings.");
await Manager(HttpStatusCode.Created, HttpMethod.Put, $"/api/pets/{pet}", new { status = "ACTIVE" });
await Manager(HttpStatusCode.Created, HttpMethod.Put, $"/api/customers/{customer}", new { status = "INACTIVE" });
await Request(HttpStatusCode.BadRequest, HttpMethod.Post, "/api/bookings", BookingBody([secondOption]));
Require(Value(await ReadBooking(mainId), "status") == "PAID", "Inactive customer must not hide historical bookings.");

await app.StopAsync();
Console.WriteLine($"Booking HTTP and provider PostgreSQL verification passed ({assertions} assertions).");
return 0;

sealed class QaAuthentication(
    IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "BookingProviderQa";
    public const string RoleHeader = "X-Booking-Qa-Role";
    public const string IdHeader = "X-Booking-Qa-Id";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(RoleHeader, out var roleHeader))
            return Task.FromResult(AuthenticateResult.NoResult());
        var role = roleHeader.ToString();
        var accountType = role == AccountRoles.Customer ? AccountTypes.Customer : AccountTypes.Staff;
        var idType = accountType == AccountTypes.Customer ? AuthClaimTypes.CustomerId : AuthClaimTypes.StaffId;
        var id = Request.Headers[IdHeader].ToString();
        Claim[] claims =
        [
            new(AuthClaimTypes.Subject, $"{accountType}:{id}"), new(AuthClaimTypes.AccountType, accountType),
            new(AuthClaimTypes.Role, role), new(idType, id)
        ];
        var identity = new ClaimsIdentity(claims, SchemeName, AuthClaimTypes.Subject, AuthClaimTypes.Role);
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName)));
    }
}
