﻿namespace DatabaseManager
{
    /// <summary>
    /// No updatable field
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class NotUpdateAttribute: Attribute
    {
    }
}