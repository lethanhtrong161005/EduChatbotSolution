using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace DataAccessLayer.Migrations;

public partial class AddPhase2ContractDataModel : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "test_responses");
        migrationBuilder.DropTable(name: "test_questions");
        migrationBuilder.DropTable(name: "experiments");

        migrationBuilder.AddColumn<int>(name: "index_availability", table: "subjects", type: "integer", nullable: false, defaultValue: 0);
        migrationBuilder.AddColumn<int>(name: "chunk_overlap", table: "subject_ai_configurations", type: "integer", nullable: true);
        migrationBuilder.AddColumn<int>(name: "chunk_size", table: "subject_ai_configurations", type: "integer", nullable: true);
        migrationBuilder.AddColumn<int>(name: "chunk_overlap", table: "global_ai_configurations", type: "integer", nullable: false, defaultValue: 200);
        migrationBuilder.AddColumn<int>(name: "chunk_size", table: "global_ai_configurations", type: "integer", nullable: false, defaultValue: 1000);
        migrationBuilder.AddColumn<int>(name: "indexed_chunk_overlap", table: "documents", type: "integer", nullable: true);
        migrationBuilder.AddColumn<int>(name: "indexed_chunk_size", table: "documents", type: "integer", nullable: true);
        migrationBuilder.AddColumn<string>(name: "indexed_chunking_strategy", table: "documents", type: "text", nullable: true);
        migrationBuilder.AddColumn<string>(name: "indexed_embedding_model", table: "documents", type: "text", nullable: true);

        migrationBuilder.AlterColumn<int>(name: "prompt_tokens", table: "chat_session_title_generation_metrics", type: "integer", nullable: true, oldClrType: typeof(int), oldType: "integer");
        migrationBuilder.AlterColumn<int>(name: "completion_tokens", table: "chat_session_title_generation_metrics", type: "integer", nullable: true, oldClrType: typeof(int), oldType: "integer");
        migrationBuilder.AlterColumn<double>(name: "tokens_per_second", table: "chat_message_generation_metrics", type: "double precision", nullable: true, oldClrType: typeof(double), oldType: "double precision");
        migrationBuilder.AlterColumn<long>(name: "time_to_first_token_ms", table: "chat_message_generation_metrics", type: "bigint", nullable: true, oldClrType: typeof(long), oldType: "bigint");
        migrationBuilder.AlterColumn<int>(name: "prompt_tokens", table: "chat_message_generation_metrics", type: "integer", nullable: true, oldClrType: typeof(int), oldType: "integer");
        migrationBuilder.AlterColumn<int>(name: "completion_tokens", table: "chat_message_generation_metrics", type: "integer", nullable: true, oldClrType: typeof(int), oldType: "integer");

        CreateExperimentTables(migrationBuilder);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "experiment_configuration_snapshots");
        migrationBuilder.DropTable(name: "test_response_contexts");
        migrationBuilder.DropTable(name: "test_responses");
        migrationBuilder.DropTable(name: "test_questions");
        migrationBuilder.DropTable(name: "experiments");

        CreateLegacyExperimentTables(migrationBuilder);

        migrationBuilder.DropColumn(name: "index_availability", table: "subjects");
        migrationBuilder.DropColumn(name: "chunk_overlap", table: "subject_ai_configurations");
        migrationBuilder.DropColumn(name: "chunk_size", table: "subject_ai_configurations");
        migrationBuilder.DropColumn(name: "chunk_overlap", table: "global_ai_configurations");
        migrationBuilder.DropColumn(name: "chunk_size", table: "global_ai_configurations");
        migrationBuilder.DropColumn(name: "indexed_chunk_overlap", table: "documents");
        migrationBuilder.DropColumn(name: "indexed_chunk_size", table: "documents");
        migrationBuilder.DropColumn(name: "indexed_chunking_strategy", table: "documents");
        migrationBuilder.DropColumn(name: "indexed_embedding_model", table: "documents");

        migrationBuilder.AlterColumn<int>(name: "prompt_tokens", table: "chat_session_title_generation_metrics", type: "integer", nullable: false, defaultValue: 0, oldClrType: typeof(int), oldType: "integer", oldNullable: true);
        migrationBuilder.AlterColumn<int>(name: "completion_tokens", table: "chat_session_title_generation_metrics", type: "integer", nullable: false, defaultValue: 0, oldClrType: typeof(int), oldType: "integer", oldNullable: true);
        migrationBuilder.AlterColumn<double>(name: "tokens_per_second", table: "chat_message_generation_metrics", type: "double precision", nullable: false, defaultValue: 0.0, oldClrType: typeof(double), oldType: "double precision", oldNullable: true);
        migrationBuilder.AlterColumn<long>(name: "time_to_first_token_ms", table: "chat_message_generation_metrics", type: "bigint", nullable: false, defaultValue: 0L, oldClrType: typeof(long), oldType: "bigint", oldNullable: true);
        migrationBuilder.AlterColumn<int>(name: "prompt_tokens", table: "chat_message_generation_metrics", type: "integer", nullable: false, defaultValue: 0, oldClrType: typeof(int), oldType: "integer", oldNullable: true);
        migrationBuilder.AlterColumn<int>(name: "completion_tokens", table: "chat_message_generation_metrics", type: "integer", nullable: false, defaultValue: 0, oldClrType: typeof(int), oldType: "integer", oldNullable: true);
    }

    private static void CreateExperimentTables(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "experiments",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                experiment_name = table.Column<string>(type: "text", nullable: false),
                subject_id = table.Column<int>(type: "integer", nullable: false),
                status = table.Column<int>(type: "integer", nullable: false),
                question_set_key = table.Column<string>(type: "text", nullable: false),
                indexed_document_count = table.Column<int>(type: "integer", nullable: false),
                affected_document_count = table.Column<int>(type: "integer", nullable: false),
                completed_question_count = table.Column<int>(type: "integer", nullable: false),
                total_question_count = table.Column<int>(type: "integer", nullable: false),
                faithfulness = table.Column<double>(type: "double precision", nullable: true),
                answer_relevancy = table.Column<double>(type: "double precision", nullable: true),
                context_precision = table.Column<double>(type: "double precision", nullable: true),
                context_recall = table.Column<double>(type: "double precision", nullable: true),
                notes = table.Column<string>(type: "text", nullable: true),
                completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                failure_reason = table.Column<string>(type: "text", nullable: true),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_experiments", x => x.id);
                table.ForeignKey("fk_experiments_subjects_subject_id", x => x.subject_id, "subjects", "id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "test_questions",
            columns: table => new
            {
                id = table.Column<int>(type: "integer", nullable: false).Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                subject_id = table.Column<int>(type: "integer", nullable: false),
                external_id = table.Column<string>(type: "text", nullable: false),
                language = table.Column<string>(type: "text", nullable: false),
                question = table.Column<string>(type: "text", nullable: false),
                ground_truth = table.Column<string>(type: "text", nullable: false),
                difficulty = table.Column<string>(type: "text", nullable: true),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_test_questions", x => x.id);
                table.ForeignKey("fk_test_questions_subjects_subject_id", x => x.subject_id, "subjects", "id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "experiment_configuration_snapshots",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                subject_id = table.Column<int>(type: "integer", nullable: false),
                subject_code = table.Column<string>(type: "text", nullable: false),
                subject_name = table.Column<string>(type: "text", nullable: false),
                chunking_strategy = table.Column<string>(type: "text", nullable: false),
                chunk_size = table.Column<int>(type: "integer", nullable: false),
                chunk_overlap = table.Column<int>(type: "integer", nullable: false),
                embedding_model = table.Column<string>(type: "text", nullable: false),
                top_k = table.Column<int>(type: "integer", nullable: false),
                similarity_threshold = table.Column<double>(type: "double precision", nullable: false),
                max_context_chunks = table.Column<int>(type: "integer", nullable: false),
                llm_model = table.Column<string>(type: "text", nullable: false),
                chat_temperature = table.Column<float>(type: "real", nullable: false),
                max_history_messages = table.Column<int>(type: "integer", nullable: false),
                chat_prompt = table.Column<string>(type: "text", nullable: false),
                context_prompt = table.Column<string>(type: "text", nullable: false),
                no_context_retrieved_prompt = table.Column<string>(type: "text", nullable: false),
                judge_model = table.Column<string>(type: "text", nullable: false),
                evaluator_prompt_version = table.Column<string>(type: "text", nullable: false),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_experiment_configuration_snapshots", x => x.id);
                table.CheckConstraint("ck_experiment_configuration_snapshots_chunk_values", "chunk_size BETWEEN 100 AND 8000 AND chunk_overlap >= 0 AND chunk_overlap < chunk_size");
                table.ForeignKey("fk_experiment_configuration_snapshots_experiments_id", x => x.id, "experiments", "id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "test_responses",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                experiment_id = table.Column<Guid>(type: "uuid", nullable: false),
                test_question_id = table.Column<int>(type: "integer", nullable: false),
                status = table.Column<int>(type: "integer", nullable: false),
                generated_answer = table.Column<string>(type: "text", nullable: true),
                faithfulness = table.Column<double>(type: "double precision", nullable: true),
                answer_relevancy = table.Column<double>(type: "double precision", nullable: true),
                context_precision = table.Column<double>(type: "double precision", nullable: true),
                context_recall = table.Column<double>(type: "double precision", nullable: true),
                explanation = table.Column<string>(type: "text", nullable: true),
                failure_reason = table.Column<string>(type: "text", nullable: true),
                prompt_tokens = table.Column<int>(type: "integer", nullable: true),
                completion_tokens = table.Column<int>(type: "integer", nullable: true),
                retrieval_time_ms = table.Column<long>(type: "bigint", nullable: true),
                time_to_first_token_ms = table.Column<long>(type: "bigint", nullable: true),
                total_response_time_ms = table.Column<long>(type: "bigint", nullable: true),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_test_responses", x => x.id);
                table.ForeignKey("fk_test_responses_experiments_experiment_id", x => x.experiment_id, "experiments", "id", onDelete: ReferentialAction.Cascade);
                table.ForeignKey("fk_test_responses_test_questions_test_question_id", x => x.test_question_id, "test_questions", "id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "test_response_contexts",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                test_response_id = table.Column<Guid>(type: "uuid", nullable: false),
                context_index = table.Column<int>(type: "integer", nullable: false),
                context_text = table.Column<string>(type: "text", nullable: false),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_test_response_contexts", x => x.id);
                table.CheckConstraint("ck_test_response_contexts_context_index", "context_index >= 0");
                table.ForeignKey("fk_test_response_contexts_test_responses_test_response_id", x => x.test_response_id, "test_responses", "id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex("ix_experiments_subject_id", "experiments", "subject_id");
        migrationBuilder.CreateIndex("ix_test_questions_subject_id_external_id", "test_questions", new[] { "subject_id", "external_id" }, unique: true);
        migrationBuilder.CreateIndex("ix_test_responses_experiment_id_test_question_id", "test_responses", new[] { "experiment_id", "test_question_id" }, unique: true);
        migrationBuilder.CreateIndex("ix_test_responses_test_question_id", "test_responses", "test_question_id");
        migrationBuilder.CreateIndex("ix_test_response_contexts_test_response_id_context_index", "test_response_contexts", new[] { "test_response_id", "context_index" }, unique: true);

        foreach (var table in new[] { "experiments", "test_questions", "experiment_configuration_snapshots", "test_responses", "test_response_contexts" })
            CreateTimestampTrigger(migrationBuilder, table);
    }

    private static void CreateLegacyExperimentTables(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "experiments",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                experiment_name = table.Column<string>(type: "text", nullable: false),
                embedding_model = table.Column<string>(type: "text", nullable: false),
                chunking_strategy = table.Column<string>(type: "text", nullable: false),
                retrieval_method = table.Column<string>(type: "text", nullable: false),
                llm_model = table.Column<string>(type: "text", nullable: false),
                average_ragas_score = table.Column<double>(type: "double precision", nullable: true),
                notes = table.Column<string>(type: "text", nullable: true),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            },
            constraints: table => table.PrimaryKey("pk_experiments", x => x.id));

        migrationBuilder.CreateTable(
            name: "test_questions",
            columns: table => new
            {
                id = table.Column<int>(type: "integer", nullable: false).Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                question = table.Column<string>(type: "text", nullable: false),
                ground_truth = table.Column<string>(type: "text", nullable: false),
                difficulty = table.Column<string>(type: "text", nullable: true),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            },
            constraints: table => table.PrimaryKey("pk_test_questions", x => x.id));

        migrationBuilder.CreateTable(
            name: "test_responses",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                experiment_id = table.Column<Guid>(type: "uuid", nullable: false),
                test_question_id = table.Column<int>(type: "integer", nullable: false),
                generated_answer = table.Column<string>(type: "text", nullable: false),
                faithfulness = table.Column<double>(type: "double precision", nullable: true),
                answer_relevancy = table.Column<double>(type: "double precision", nullable: true),
                context_precision = table.Column<double>(type: "double precision", nullable: true),
                context_recall = table.Column<double>(type: "double precision", nullable: true),
                latency_time_to_first_token_ms = table.Column<int>(type: "integer", nullable: false),
                latency_time_per_output_token_ms = table.Column<int>(type: "integer", nullable: false),
                latency_total_generation_time_ms = table.Column<int>(type: "integer", nullable: false),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_test_responses", x => x.id);
                table.ForeignKey("fk_test_responses_experiments_experiment_id", x => x.experiment_id, "experiments", "id", onDelete: ReferentialAction.Cascade);
                table.ForeignKey("fk_test_responses_test_questions_test_question_id", x => x.test_question_id, "test_questions", "id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex("ix_test_responses_experiment_id", "test_responses", "experiment_id");
        migrationBuilder.CreateIndex("ix_test_responses_test_question_id", "test_responses", "test_question_id");
        foreach (var table in new[] { "experiments", "test_questions", "test_responses" })
            CreateTimestampTrigger(migrationBuilder, table);
    }

    private static void CreateTimestampTrigger(MigrationBuilder migrationBuilder, string table)
    {
        migrationBuilder.Sql($$"""
            CREATE TRIGGER "update_timestamp"
                BEFORE UPDATE ON "{{table}}"
                FOR EACH ROW
                EXECUTE FUNCTION "update_timestamp"();
            """);
    }
}
