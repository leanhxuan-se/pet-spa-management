using PetSpa.Modules.Resource.Domain.Enums;

namespace PetSpa.Modules.Resource.Application.Services;

internal static class ResourceValidation
{
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

    public static void Status(ResourceStatus status, Dictionary<string, string[]> errors)
    {
        if (!Enum.IsDefined(status) || status == ResourceStatus.DELETED)
            errors["Status"] = ["Trạng thái phải là ACTIVE, MAINTAINED hoặc INACTIVE. Dùng DELETE để xóa."];
    }

    public static void ParentId(long id, string field, Dictionary<string, string[]> errors)
    {
        if (id <= 0)
            errors[field] = ["Id phải lớn hơn 0."];
    }
}
