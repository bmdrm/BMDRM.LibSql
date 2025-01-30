// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore.Storage;

namespace Microsoft.EntityFrameworkCore.LibSql.Helpers
{
    /// <summary>
    /// Provides SQL generation functionality for LibSQL, extending the relational SQL generation helper.
    /// This class overrides the <see cref="DelimitIdentifier"/> method to customize the delimiter behavior.
    /// </summary>
    public class LibSqlSqlGenerationHelper : RelationalSqlGenerationHelper
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LibSqlSqlGenerationHelper"/> class with the specified dependencies.
        /// </summary>
        /// <param name="dependencies">The dependencies required for relational SQL generation.</param>
        public LibSqlSqlGenerationHelper(RelationalSqlGenerationHelperDependencies dependencies)
            : base(dependencies) { }

        /// <summary>
        /// Delimits the specified identifier with appropriate quotes for the LibSQL database.
        /// This method overrides the base method to apply custom delimiter logic.
        /// </summary>
        /// <param name="name">The name of the identifier (e.g., table or column name).</param>
        /// <param name="schema">The schema the identifier belongs to, or null if not applicable.</param>
        /// <returns>A string with the delimited identifier.</returns>
        public override string DelimitIdentifier(string name, string? schema)
        {
            return base.DelimitIdentifier(name, schema);
        }
    }
}

