using LinkVaultApi.Dtos;

namespace LinkVaultApi.Services;

public interface IChatService
{
    Task<ChatMessageDto> SendMessageAsync(Guid directoryId, Guid userId, SendMessageRequestDto request);
    Task<ChatMessageDto?> EditMessageAsync(Guid messageId, Guid userId, EditMessageRequestDto request);
    Task<bool> DeleteMessageAsync(Guid messageId, Guid userId);
    Task<ChatHistoryResponseDto> GetChatHistoryAsync(Guid directoryId, ChatHistoryRequestDto request);
    Task<List<ChatMessageDto>> GetPinnedMessagesAsync(Guid directoryId);
    Task<bool> PinMessageAsync(Guid messageId, Guid userId);
    Task<bool> UnpinMessageAsync(Guid messageId, Guid userId);
    Task<List<OnlineUserDto>> GetOnlineUsersAsync(Guid directoryId);
}
