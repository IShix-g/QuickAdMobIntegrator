
using System;

namespace QuickAdMobIntegrator.Admob.Editor
{
    public struct ValidationErrorState : IEquatable<ValidationErrorState>
    {
        public readonly ValidationErrorType Type;
        public readonly string Message;
        public bool IsValid => Type != ValidationErrorType.None && !string.IsNullOrWhiteSpace(Message);
        
        public ValidationErrorState(ValidationErrorType type, string message)
        {
            Type = type;
            Message = message;
        }

        public bool Equals(ValidationErrorState other)
            => Type == other.Type && Message == other.Message;

        public override bool Equals(object obj)
            => obj is ValidationErrorState other && Equals(other);

        public override int GetHashCode()
            => HashCode.Combine((int) Type, Message);

        public static bool operator ==(ValidationErrorState left, ValidationErrorState right)
            => left.Equals(right);

        public static bool operator !=(ValidationErrorState left, ValidationErrorState right)
            => !left.Equals(right);
        
        public static readonly ValidationErrorState Empty = new (ValidationErrorType.None, default);
    }
}