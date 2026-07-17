using DataAccessLayer.Migrations;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace UnitTests;

[TestFixture]
public sealed class NormalizePersistedOrdinalsMigrationTests
{
    private static readonly (string Table, string Index, string Parent)[] Collections =
    [
        ("parsed_sections", "section_index", "document_id"),
        ("chunks", "chunk_index", "document_id"),
        ("chat_message_contexts", "context_index", "chat_message_id"),
        ("test_response_contexts", "context_index", "test_response_id"),
        ("citation_occurrences", "occurrence_index", "citation_id"),
    ];

    [Test]
    public void Up_PreflightsExactZeroBasedSequencesBeforeIncrementingAndConstraining()
    {
        var operations = new TestableMigration().BuildUpOperations();
        var sql = operations.OfType<SqlOperation>().ToArray();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(sql, Has.Length.EqualTo(10));
            foreach (var (table, index, parent) in Collections)
            {
                Assert.That(sql.Take(5).Single(operation => operation.Sql.Contains($"FROM \"{table}\"", StringComparison.Ordinal)).Sql, Does.Contain($"MIN(\"{index}\") <> 0").And.Contain($"GROUP BY \"{parent}\"").And.Contain($"Cannot normalize {table}"));
                Assert.That(sql.Skip(5).Any(operation => operation.Sql == $"UPDATE {table} SET {index} = {index} + 1;"), Is.True);
                Assert.That(operations.OfType<AddCheckConstraintOperation>().Any(operation => operation.Table == table && operation.Sql == $"{index} >= 1"), Is.True);
            }

            Assert.That(operations.OfType<CreateIndexOperation>().Any(operation => operation.Table == "parsed_sections" && operation.IsUnique && operation.Columns.SequenceEqual(["document_id", "section_index"])), Is.True);
            Assert.That(operations.OfType<CreateIndexOperation>().Any(operation => operation.Table == "chunks" && operation.IsUnique && operation.Columns.SequenceEqual(["document_id", "chunk_index"])), Is.True);
            Assert.That(operations.OfType<CreateIndexOperation>().Any(operation => operation.Table == "citation_occurrences" && operation.IsUnique && operation.Columns.SequenceEqual(["citation_id", "occurrence_index"])), Is.True);
            Assert.That(operations.OfType<DropIndexOperation>().Any(operation => operation.Table == "chat_message_contexts" && operation.Name == "ix_chat_message_contexts_chat_message_id_context_index"), Is.True);
            Assert.That(operations.OfType<CreateIndexOperation>().Any(operation => operation.Table == "chat_message_contexts" && operation.IsUnique && operation.Columns.SequenceEqual(["chat_message_id", "context_index"])), Is.True);
            Assert.That(operations.OfType<DropIndexOperation>().Any(operation => operation.Table == "test_response_contexts" && operation.Name == "ix_test_response_contexts_test_response_id_context_index"), Is.True);
            Assert.That(operations.OfType<CreateIndexOperation>().Any(operation => operation.Table == "test_response_contexts" && operation.IsUnique && operation.Columns.SequenceEqual(["test_response_id", "context_index"])), Is.True);
        }
    }

    [Test]
    public void Down_PreflightsExactOneBasedSequencesBeforeRemovingConstraintsAndDecrementing()
    {
        var operations = new TestableMigration().BuildDownOperations();
        var sql = operations.OfType<SqlOperation>().ToArray();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(sql, Has.Length.EqualTo(10));
            foreach (var (table, index, parent) in Collections)
            {
                Assert.That(sql.Take(5).Single(operation => operation.Sql.Contains($"FROM \"{table}\"", StringComparison.Ordinal)).Sql, Does.Contain($"MIN(\"{index}\") <> 1").And.Contain($"GROUP BY \"{parent}\"").And.Contain($"Cannot roll back {table}"));
                Assert.That(sql.Skip(5).Any(operation => operation.Sql == $"UPDATE {table} SET {index} = {index} - 1;"), Is.True);
            }

            Assert.That(operations.OfType<DropIndexOperation>().Any(operation => operation.Table == "chat_message_contexts" && operation.Name == "ix_chat_message_contexts_chat_message_id_context_index"), Is.True);
            Assert.That(operations.OfType<CreateIndexOperation>().Any(operation => operation.Table == "chat_message_contexts" && operation.IsUnique && operation.Columns.SequenceEqual(["chat_message_id", "context_index"])), Is.True);
            Assert.That(operations.OfType<DropIndexOperation>().Any(operation => operation.Table == "test_response_contexts" && operation.Name == "ix_test_response_contexts_test_response_id_context_index"), Is.True);
            Assert.That(operations.OfType<CreateIndexOperation>().Any(operation => operation.Table == "test_response_contexts" && operation.IsUnique && operation.Columns.SequenceEqual(["test_response_id", "context_index"])), Is.True);
        }
    }

    private sealed class TestableMigration : NormalizePersistedOrdinalsToOneBased
    {
        public IReadOnlyList<MigrationOperation> BuildUpOperations()
        {
            var builder = new MigrationBuilder("Npgsql.EntityFrameworkCore.PostgreSQL");
            base.Up(builder);
            return builder.Operations;
        }

        public IReadOnlyList<MigrationOperation> BuildDownOperations()
        {
            var builder = new MigrationBuilder("Npgsql.EntityFrameworkCore.PostgreSQL");
            base.Down(builder);
            return builder.Operations;
        }
    }
}
