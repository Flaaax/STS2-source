namespace MegaCrit.Sts2.Core.Saves.Migrations;

/// <summary>
/// Strongly typed interface for migrations that operate on a specific save type.
/// </summary>
/// <typeparam name="T">The save type to migrate</typeparam>
public interface IMigration<T> : IMigration where T : ISaveSchema
{
}
