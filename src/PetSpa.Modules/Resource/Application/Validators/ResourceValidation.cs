// Định nghĩa quy tắc kiểm tra dữ liệu ResourceValidation.
using PetSpa.Modules.Resource.Domain.Enums;

namespace PetSpa.Modules.Resource.Application.Validators;

internal static class ResourceValidation
{
    // Kiểm tra tên và trạng thái.
    public static Dictionary<string, string[]> NameAndStatus(
        string? name, string field, int maxLength, ResourceStatus status)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(name))
            errors[field] = ["Tên không được để trống."];
        else if (name.Trim().Length > maxLength)
            errors[field] = [$"Tên không được vượt quá {maxLength} ký tự."];

        Status(status, errors);
        return errors;
    }

    // Kiểm tra trạng thái.
    public static void Status(ResourceStatus status, Dictionary<string, string[]> errors)
    {
        if (!Enum.IsDefined(status))
            errors["Status"] = ["Trạng thái phải là ACTIVE hoặc INACTIVE."];
    }

    // Kiểm tra mã đối tượng liên quan.
    public static void ParentId(long id, string field, Dictionary<string, string[]> errors)
    {
        if (id <= 0)
            errors[field] = ["Id phải lớn hơn 0."];
    }
}
