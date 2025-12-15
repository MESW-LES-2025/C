using Consilium.Domain.Enums;

namespace Consilium.API.Dtos;

// ============================================
// REQUEST DTOs (for incoming data)
// ============================================

/// <summary>
/// Request DTO for creating a new client
/// </summary>
public record CreateClientRequest(
    string Email,
    string Password,
    string Name,
    string NIF,
    string? Address,
    // Optional phone info (single/main phone)
    string? PhoneNumber,
    short? PhoneCountryCode,
    bool? PhoneIsMain
);

/// <summary>
/// Request DTO for user login
/// </summary>
public record LoginRequest(
    string Email,
    string Password
);

/// <summary>
/// Request DTO for updating user information
/// </summary>
public record UpdateUserRequest(
    string? Name,
    string? Email,
    string? Password
);

/// <summary>
/// Request DTO for updating client information (updates both User and Client data)
/// </summary>
public record UpdateClientRequest(
    string? Name,
    string? Email,
    string? Password,
    string? Address,
    string? NIF,
    bool? IsActive,
    // Optional phone update
    string? PhoneNumber,
    short? PhoneCountryCode,
    bool? PhoneIsMain
);

/// <summary>
/// Request DTO for creating a new lawyer
/// </summary>
public record CreateLawyerRequest(
    string Email,
    string Password,
    string Name,
    string NIF,
    string ProfessionalRegister,
    // Optional phone info (single/main phone)
    string? PhoneNumber,
    short? PhoneCountryCode,
    bool? PhoneIsMain
);

/// <summary>
/// Request DTO for updating lawyer information (updates both User and Lawyer data)
/// </summary>
public record UpdateLawyerRequest(
    string? Name,
    string? Email,
    string? Password,
    string? ProfessionalRegister,
    string? NIF,
    // Optional phone update
    string? PhoneNumber,
    short? PhoneCountryCode,
    bool? PhoneIsMain,
    bool? IsActive
);

/// <summary>
/// Request DTO for creating a new admin
/// </summary>
public record CreateAdminRequest(
    string Email,
    string Password,
    string Name,
    string NIF,
    // Optional phone info (single/main phone)
    string? PhoneNumber,
    short? PhoneCountryCode,
    bool? PhoneIsMain
);

/// <summary>
/// Request DTO for updating admin information (updates both User and Admin data)
/// </summary>
public record UpdateAdminRequest(
    string? Name,
    string? Email,
    string? Password,
    // Optional phone update
    string? PhoneNumber,
    short? PhoneCountryCode,
    bool? PhoneIsMain
);

// ============================================
// RESPONSE DTOs (for outgoing data)
// ============================================

/// <summary>
/// Response DTO for User data
/// </summary>
public record UserResponse(
    Guid Id,
    string Email,
    string Name,
    UserStatus Status
);

/// <summary>
/// Response DTO for Client data (User + Client info)
/// </summary>
public record ClientResponse(
    Guid Id,
    string Email,
    string Name,
    UserStatus Status,
    string NIF,
    string? Address,
    string? Phone,
    short? PhoneCountryCode
);

/// <summary>
/// Response DTO for Lawyer data (User + Lawyer info)
/// </summary>
public record LawyerResponse(
    Guid Id,
    string Email,
    string Name,
    UserStatus Status,
    string NIF,
    string ProfessionalRegister,
    string? Phone,
    short? PhoneCountryCode
);

/// <summary>
/// Response DTO for Admin data (User + Admin info)
/// </summary>
public record AdminResponse(
    Guid Id,
    string Email,
    string Name,
    UserStatus Status,
    string NIF,
    DateTime StartedAt,
    string? Phone,
    short? PhoneCountryCode
);

/// <summary>
/// Response DTO for successful login
/// </summary>
public record LoginResponse(
    string Token,
    Guid UserId,
    string Email,
    string Name,
    UserStatus Status
);

// =========================
// Process DTOs
// =========================
public record CreateProcessRequest(
    string Name,
    string Number,
    Guid ClientId,
    Guid LawyerId,
    string? AdversePartName,
    string? OpposingCounselName,
    short Priority,
    string CourtInfo,
    int ProcessTypePhaseId,
    int ProcessStatusId,
    DateTime? NextHearingDate,
    string? Description
);

public record UpdateProcessRequest(
    string? Name,
    string? Number,
    Guid? ClientId,
    Guid? LawyerId,
    string? AdversePartName,
    string? OpposingCounselName,
    short? Priority,
    string? CourtInfo,
    int? ProcessTypePhaseId,
    int? ProcessStatusId,
    DateTime? NextHearingDate,
    string? Description,
    DateTime? ClosedAt
);

/// <summary>
/// Multipart form request for creating a process with file uploads
/// </summary>
public class CreateProcessWithDocumentsRequest
{
    public string Name { get; set; } = string.Empty;
    public string Number { get; set; } = string.Empty;
    public Guid ClientId { get; set; }
    public Guid LawyerId { get; set; }
    public string? AdversePartName { get; set; }
    public string? OpposingCounselName { get; set; }
    public short Priority { get; set; }
    public string CourtInfo { get; set; } = string.Empty;
    public int ProcessTypePhaseId { get; set; }
    public int ProcessStatusId { get; set; }
    public DateTime? NextHearingDate { get; set; }
    public string? Description { get; set; }

    // Files uploaded via multipart form-data (use IFormFileCollection for proper binding)
    public IFormFileCollection? Files { get; set; }
}

/// <summary>
/// Multipart form request for updating a process with file uploads and optional deletion of existing documents
/// </summary>
public class UpdateProcessWithDocumentsRequest
{
    public string? Name { get; set; }
    public string? Number { get; set; }
    // Accept as string from form binding, parse in endpoint if non-empty
    public string? ClientId { get; set; }
    public string? LawyerId { get; set; }
    public string? AdversePartName { get; set; }
    public string? OpposingCounselName { get; set; }
    // Accept as string from form binding, parse in endpoint if non-empty
    public string? Priority { get; set; }
    public string? CourtInfo { get; set; }
    // Accept as string from form binding, parse in endpoint if non-empty
    public string? ProcessTypePhaseId { get; set; }
    public string? ProcessStatusId { get; set; }
    // Accept as string from form binding, parse in endpoint if non-empty
    public string? NextHearingDate { get; set; }
    public string? Description { get; set; }
    public string? ClosedAt { get; set; }

    // Files to add (use IFormFileCollection for proper binding)
    public IFormFileCollection? Files { get; set; }

    // Document IDs to delete - can be sent as multiple form fields or a list
    public List<string>? DeletedDocumentIds { get; set; }
}

public record ProcessResponse(
    Guid ProcessId,
    string Name,
    string Number,
    Guid ClientId,
    string? ClientName,
    Guid LawyerId,
    string? LawyerName,
    string? AdversePartName,
    string? OpposingCounselName,
    DateTime CreatedAt,
    DateTime? ClosedAt,
    short Priority,
    string CourtInfo,
    int ProcessTypePhaseId,
    int ProcessStatusId,
    string? Description,
    DateTime? NextHearingDate
);

public record DocumentResponse(
    Guid DocumentId,
    string FileName,
    string FileMimeType,
    long FileSize,
    DateTime CreatedAt,
    string DownloadUrl
);

public record ProcessWithDocumentsResponse(
    Guid ProcessId,
    string Name,
    string Number,
    Guid ClientId,
    string? ClientName,
    Guid LawyerId,
    string? LawyerName,
    string? AdversePartName,
    string? OpposingCounselName,
    DateTime CreatedAt,
    DateTime? ClosedAt,
    short Priority,
    string CourtInfo,
    int ProcessTypePhaseId,
    int ProcessStatusId,
    string? Description,
    DateTime? NextHearingDate,
    List<DocumentResponse> Documents
);

// ========== Lookup responses ===========
public record ProcessTypeResponse(
    int Id,
    string Name,
    bool IsActive
);

public record ProcessPhaseResponse(
    int Id,
    string Name,
    string? Description,
    bool IsActive
);

public record ProcessStatusResponse(
    int Id,
    string Name,
    bool IsFinal,
    bool IsDefault,
    bool IsActive
);

public record ProcessTypePhaseResponse(
    int Id,
    int ProcessTypeId,
    string ProcessTypeName,
    int ProcessPhaseId,
    string ProcessPhaseName,
    short Order,
    bool IsOptional,
    bool IsActive
);
