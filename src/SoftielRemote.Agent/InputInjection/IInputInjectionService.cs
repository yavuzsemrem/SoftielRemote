using SoftielRemote.Core.Messages;

namespace SoftielRemote.Agent.InputInjection;

/// <summary>
/// Input injection servisi interface'i (mouse ve klavye kontrolü).
/// </summary>
public interface IInputInjectionService
{
    /// <summary>
    /// Remote input mesajını işler ve işletim sistemine enjekte eder.
    /// </summary>
    Task<bool> InjectInputAsync(RemoteInputMessage inputMessage);

    /// <summary>
    /// Input injection'ın aktif olup olmadığını kontrol eder.
    /// </summary>
    bool IsEnabled { get; set; }
}



