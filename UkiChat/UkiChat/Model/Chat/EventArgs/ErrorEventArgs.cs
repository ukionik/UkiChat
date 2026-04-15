using System;

namespace UkiChat.Model.Chat.EventArgs;

public class ErrorEventArgs : System.EventArgs
{
    public string Message { get; init; } = string.Empty;
    public Exception? Exception { get; init; }    
}