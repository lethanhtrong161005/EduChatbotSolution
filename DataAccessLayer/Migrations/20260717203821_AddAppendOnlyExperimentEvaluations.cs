using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class AddAppendOnlyExperimentEvaluations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_test_responses_experiment_id_test_question_id",
                table: "test_responses");

            migrationBuilder.RenameColumn(
                name: "judge_model",
                table: "experiment_configuration_snapshots",
                newName: "evaluator_llm_model");

            migrationBuilder.AddColumn<Guid>(
                name: "current_evaluation_attempt_id",
                table: "test_responses",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "evaluator_embedding_model",
                table: "experiment_configuration_snapshots",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "evaluator_embedding_provider",
                table: "experiment_configuration_snapshots",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "evaluator_llm_provider",
                table: "experiment_configuration_snapshots",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(name: "evaluator_metric_set_key", table: "experiment_configuration_snapshots", type: "text", nullable: false, defaultValue: "ragas-rag-core-v1");

            migrationBuilder.CreateTable(
                name: "test_response_evaluation_attempts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    test_response_id = table.Column<Guid>(type: "uuid", nullable: false),
                    attempt_number = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    evaluator_family = table.Column<string>(type: "text", nullable: false),
                    contract_version = table.Column<string>(type: "text", nullable: false),
                    service_version = table.Column<string>(type: "text", nullable: false),
                    ragas_version = table.Column<string>(type: "text", nullable: false),
                    prompt_version = table.Column<string>(type: "text", nullable: false),
                    language = table.Column<string>(type: "text", nullable: false),
                    llm_provider = table.Column<string>(type: "text", nullable: false),
                    llm_model = table.Column<string>(type: "text", nullable: false),
                    embedding_provider = table.Column<string>(type: "text", nullable: false),
                    embedding_model = table.Column<string>(type: "text", nullable: false),
                    metric_set_key = table.Column<string>(type: "text", nullable: false),
                    evaluator_profile_key = table.Column<string>(type: "text", nullable: false),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    summary = table.Column<string>(type: "text", nullable: true),
                    failure_reason = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_test_response_evaluation_attempts", x => x.id);
                    table.CheckConstraint("ck_test_response_evaluation_attempts_attempt_number", "attempt_number >= 1");
                    table.ForeignKey(
                        name: "fk_test_response_evaluation_attempts_test_responses_test_respo",
                        column: x => x.test_response_id,
                        principalTable: "test_responses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "test_response_evaluation_metrics",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    evaluation_attempt_id = table.Column<Guid>(type: "uuid", nullable: false),
                    metric_name = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    score = table.Column<double>(type: "double precision", nullable: true),
                    reason = table.Column<string>(type: "text", nullable: true),
                    error_code = table.Column<string>(type: "text", nullable: true),
                    error_message = table.Column<string>(type: "text", nullable: true),
                    retry_count = table.Column<int>(type: "integer", nullable: false),
                    duration_ms = table.Column<long>(type: "bigint", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_test_response_evaluation_metrics", x => x.id);
                    table.CheckConstraint("ck_test_response_evaluation_metrics_retry_count", "retry_count >= 0");
                    table.CheckConstraint("ck_test_response_evaluation_metrics_score", "score IS NULL OR (score >= 0 AND score <= 1)");
                    table.ForeignKey(
                        name: "fk_test_response_evaluation_metrics_test_response_evaluation_a",
                        column: x => x.evaluation_attempt_id,
                        principalTable: "test_response_evaluation_attempts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.Sql(
                """
                INSERT INTO "test_response_evaluation_attempts" (
                    "id", "test_response_id", "attempt_number", "status", "evaluator_family", "contract_version", "service_version", "ragas_version", "prompt_version", "language",
                    "llm_provider", "llm_model", "embedding_provider", "embedding_model", "metric_set_key", "evaluator_profile_key", "completed_at", "summary", "failure_reason", "created_at")
                SELECT response."id", response."id", 1,
                    CASE WHEN response."faithfulness" IS NOT NULL AND response."answer_relevancy" IS NOT NULL AND response."context_precision" IS NOT NULL AND response."context_recall" IS NOT NULL THEN 2
                         WHEN response."faithfulness" IS NOT NULL OR response."answer_relevancy" IS NOT NULL OR response."context_precision" IS NOT NULL OR response."context_recall" IS NOT NULL THEN 3 ELSE 4 END,
                    'csharp-ragas-style', 'legacy-csharp-ragas-style-v1', '', '', configuration."evaluator_prompt_version", COALESCE(response."question_language", ''),
                    '', configuration."evaluator_llm_model", '', '', 'legacy-csharp-ragas-style-v1',
                    concat('csharp-ragas-style|legacy-csharp-ragas-style-v1|', configuration."evaluator_prompt_version", '|', configuration."evaluator_llm_model"),
                    experiment."completed_at", response."explanation",
                    CASE WHEN response."faithfulness" IS NULL AND response."answer_relevancy" IS NULL AND response."context_precision" IS NULL AND response."context_recall" IS NULL THEN 'Legacy evaluation contained no persisted score.' ELSE NULL END,
                    response."created_at"
                FROM "test_responses" response
                JOIN "experiments" experiment ON experiment."id" = response."experiment_id"
                JOIN "experiment_configuration_snapshots" configuration ON configuration."id" = response."experiment_id"
                WHERE response."status" = 2 OR response."faithfulness" IS NOT NULL OR response."answer_relevancy" IS NOT NULL OR response."context_precision" IS NOT NULL OR response."context_recall" IS NOT NULL OR response."explanation" IS NOT NULL;

                INSERT INTO "test_response_evaluation_metrics" ("id", "evaluation_attempt_id", "metric_name", "status", "score", "error_code", "error_message", "retry_count", "created_at")
                SELECT gen_random_uuid(), response."id", metric."name", CASE WHEN metric."score" IS NULL THEN 3 ELSE 2 END, metric."score",
                    CASE WHEN metric."score" IS NULL THEN 'legacy_missing' ELSE NULL END, CASE WHEN metric."score" IS NULL THEN 'The legacy evaluator did not persist this metric.' ELSE NULL END, 0, response."created_at"
                FROM "test_responses" response
                JOIN "test_response_evaluation_attempts" attempt ON attempt."id" = response."id"
                CROSS JOIN LATERAL (VALUES
                    ('faithfulness', response."faithfulness"), ('answer_relevancy', response."answer_relevancy"),
                    ('context_precision', response."context_precision"), ('context_recall', response."context_recall")) metric("name", "score");

                UPDATE "test_responses" response SET "current_evaluation_attempt_id" = attempt."id"
                FROM "test_response_evaluation_attempts" attempt WHERE attempt."test_response_id" = response."id" AND attempt."attempt_number" = 1;
                """);

            migrationBuilder.DropColumn(name: "answer_relevancy", table: "test_responses");
            migrationBuilder.DropColumn(name: "context_precision", table: "test_responses");
            migrationBuilder.DropColumn(name: "context_recall", table: "test_responses");
            migrationBuilder.DropColumn(name: "explanation", table: "test_responses");
            migrationBuilder.DropColumn(name: "faithfulness", table: "test_responses");
            migrationBuilder.DropColumn(name: "answer_relevancy", table: "experiments");
            migrationBuilder.DropColumn(name: "context_precision", table: "experiments");
            migrationBuilder.DropColumn(name: "context_recall", table: "experiments");
            migrationBuilder.DropColumn(name: "faithfulness", table: "experiments");

            migrationBuilder.CreateIndex(
                name: "ix_test_responses_current_evaluation_attempt_id",
                table: "test_responses",
                column: "current_evaluation_attempt_id");

            migrationBuilder.CreateIndex(
                name: "ix_test_responses_experiment_id_source_question_id",
                table: "test_responses",
                columns: new[] { "experiment_id", "source_question_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_test_response_evaluation_attempts_test_response_id_attempt_",
                table: "test_response_evaluation_attempts",
                columns: new[] { "test_response_id", "attempt_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_test_response_evaluation_metrics_evaluation_attempt_id_metr",
                table: "test_response_evaluation_metrics",
                columns: new[] { "evaluation_attempt_id", "metric_name" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_test_responses_test_response_evaluation_attempts_current_ev",
                table: "test_responses",
                column: "current_evaluation_attempt_id",
                principalTable: "test_response_evaluation_attempts",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_test_responses_test_response_evaluation_attempts_current_ev",
                table: "test_responses");

            migrationBuilder.AddColumn<double>(
                name: "answer_relevancy",
                table: "test_responses",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "context_precision",
                table: "test_responses",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "context_recall",
                table: "test_responses",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "explanation",
                table: "test_responses",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "faithfulness",
                table: "test_responses",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "answer_relevancy",
                table: "experiments",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "context_precision",
                table: "experiments",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "context_recall",
                table: "experiments",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "faithfulness",
                table: "experiments",
                type: "double precision",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE "test_responses" response SET
                    "faithfulness" = metric."faithfulness",
                    "answer_relevancy" = metric."answer_relevancy",
                    "context_precision" = metric."context_precision",
                    "context_recall" = metric."context_recall",
                    "explanation" = COALESCE(attempt."summary", attempt."failure_reason")
                FROM "test_response_evaluation_attempts" attempt
                LEFT JOIN LATERAL (
                    SELECT
                        max(result."score") FILTER (WHERE result."metric_name" = 'faithfulness' AND result."status" = 2) AS "faithfulness",
                        max(result."score") FILTER (WHERE result."metric_name" = 'answer_relevancy' AND result."status" = 2) AS "answer_relevancy",
                        max(result."score") FILTER (WHERE result."metric_name" = 'context_precision' AND result."status" = 2) AS "context_precision",
                        max(result."score") FILTER (WHERE result."metric_name" = 'context_recall' AND result."status" = 2) AS "context_recall"
                    FROM "test_response_evaluation_metrics" result
                    WHERE result."evaluation_attempt_id" = attempt."id"
                ) metric ON TRUE
                WHERE attempt."id" = response."current_evaluation_attempt_id";
                """);

            migrationBuilder.Sql(
                """
                UPDATE "experiments" experiment SET
                    "faithfulness" = aggregate."faithfulness",
                    "answer_relevancy" = aggregate."answer_relevancy",
                    "context_precision" = aggregate."context_precision",
                    "context_recall" = aggregate."context_recall"
                FROM (
                    SELECT response."experiment_id",
                        avg(metric."score") FILTER (WHERE metric."metric_name" = 'faithfulness' AND metric."status" = 2) AS "faithfulness",
                        avg(metric."score") FILTER (WHERE metric."metric_name" = 'answer_relevancy' AND metric."status" = 2) AS "answer_relevancy",
                        avg(metric."score") FILTER (WHERE metric."metric_name" = 'context_precision' AND metric."status" = 2) AS "context_precision",
                        avg(metric."score") FILTER (WHERE metric."metric_name" = 'context_recall' AND metric."status" = 2) AS "context_recall"
                    FROM "test_responses" response
                    JOIN "test_response_evaluation_metrics" metric ON metric."evaluation_attempt_id" = response."current_evaluation_attempt_id"
                    GROUP BY response."experiment_id"
                ) aggregate
                WHERE aggregate."experiment_id" = experiment."id";
                """);

            migrationBuilder.DropIndex(name: "ix_test_responses_current_evaluation_attempt_id", table: "test_responses");
            migrationBuilder.DropIndex(name: "ix_test_responses_experiment_id_source_question_id", table: "test_responses");
            migrationBuilder.DropColumn(name: "current_evaluation_attempt_id", table: "test_responses");
            migrationBuilder.DropTable(name: "test_response_evaluation_metrics");
            migrationBuilder.DropTable(name: "test_response_evaluation_attempts");
            migrationBuilder.DropColumn(name: "evaluator_embedding_model", table: "experiment_configuration_snapshots");
            migrationBuilder.DropColumn(name: "evaluator_embedding_provider", table: "experiment_configuration_snapshots");
            migrationBuilder.DropColumn(name: "evaluator_llm_provider", table: "experiment_configuration_snapshots");
            migrationBuilder.DropColumn(name: "evaluator_metric_set_key", table: "experiment_configuration_snapshots");
            migrationBuilder.RenameColumn(name: "evaluator_llm_model", table: "experiment_configuration_snapshots", newName: "judge_model");

            migrationBuilder.CreateIndex(
                name: "ix_test_responses_experiment_id_test_question_id",
                table: "test_responses",
                columns: new[] { "experiment_id", "test_question_id" },
                unique: true);
        }
    }
}
