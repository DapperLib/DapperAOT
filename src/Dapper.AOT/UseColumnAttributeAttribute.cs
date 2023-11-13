using System;

namespace Dapper
{
    /// <summary>
    /// Specifies whether to use [System.ComponentModel.DataAnnotations.Schema.ColumnAttribute] for additional behavioral configuration
    /// </summary>
    public class UseColumnAttributeAttribute : Attribute
    {
        /// <param name="enable">enable usage of [Column] attribute</param>
        public UseColumnAttributeAttribute(bool enable = true)
        {
        }
    }
}
