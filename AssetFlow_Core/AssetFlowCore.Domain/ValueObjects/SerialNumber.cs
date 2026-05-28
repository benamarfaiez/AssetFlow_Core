using System;

namespace AssetFlowCore.Domain.ValueObjects;

public sealed class SerialNumber : IEquatable<SerialNumber>
{
    public string Value { get; }

    private SerialNumber(string value)
    {
        Value = value;
    }

    public static SerialNumber Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Le numéro de série ne peut pas être vide.", nameof(value));

        if (value.Length < 5 || value.Length > 50)
            throw new ArgumentException("Le numéro de série doit contenir entre 5 et 50 caractères.", nameof(value));

        return new SerialNumber(value.Trim().ToUpperInvariant());
    }

    public bool Equals(SerialNumber? other) => other != null && Value == other.Value;
    public override bool Equals(object? obj) => obj is SerialNumber other && Equals(other);
    public override int GetHashCode() => Value.GetHashCode();

    public static bool operator ==(SerialNumber? left, SerialNumber? right) => Equals(left, right);
    public static bool operator !=(SerialNumber? left, SerialNumber? right) => !Equals(left, right);
}