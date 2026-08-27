using System;

namespace EventGraph
{
    /// <summary>
    /// Builds canonical node identity keys.
    /// </summary>
    public static class GraphKey
    {
        public static string Of(string type, string name)
        {
            if (string.IsNullOrWhiteSpace(type))
            {
                throw new ArgumentException("Node type cannot be empty.", nameof(type));
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Node name cannot be empty.", nameof(name));
            }

            return $"{type}::{name}";
        }
    }
}
