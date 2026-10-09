using System;

namespace TenveoPtz.Core.Visca;

/// <summary>Raised when the camera answers a command with a VISCA error message.</summary>
public sealed class ViscaException : Exception
{
    public ViscaException(string commandName, byte errorCode)
        : base($"{commandName}: {Describe(errorCode)} (VISCA error 0x{errorCode:X2})")
    {
        CommandName = commandName;
        ErrorCode = errorCode;
    }

    public string CommandName { get; }

    public byte ErrorCode { get; }

    private static string Describe(byte errorCode) => errorCode switch
    {
        0x01 => "message length error",
        0x02 => "syntax error",
        0x03 => "command buffer full",
        0x04 => "command cancelled",
        0x05 => "no socket",
        0x41 => "command not executable",
        _ => "camera error",
    };
}
