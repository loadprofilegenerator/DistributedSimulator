using System;
using System.Runtime.Serialization;
using JetBrains.Annotations;

namespace CommonDistSimFrame
{
    [Serializable]
    public class DistSimException : Exception
    {
        [UsedImplicitly]
        public DistSimException()
        {
        }

        public DistSimException([NotNull] string message) : base(message)
        {
        }

        public DistSimException([NotNull] string message, [CanBeNull] Exception innerException) : base(message, innerException)
        {
        }

        protected DistSimException([NotNull] SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}