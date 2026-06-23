using Domain.Common;

namespace Domain.Entities;

public class GlobalAiConfiguration : CategoryLikeEntity
{
    public string ChunkingStrategy { get; set; } = "FixedLength";

    public string EmbeddingModel { get; set; } = EmbeddingModelNames.BgeM3_Latest;

    public int TopK { get; set; } = 15;

    public double SimilarityThreshold { get; set; } = 0.6;

    public string LlmModel { get; set; } = ChatModelNames.Qwen3_5;

    public float Temperature { get; set; } = 0.3F;

    public string SystemPrompt { get; set; } =
        """
        You are EduChatAI, an educational assistant.

        Your primary responsibility is answering questions using the
        retrieved course materials provided in context.

        Rules:

        - Use retrieved context whenever it is relevant.
        - Do not invent facts that are not supported by context.
        - If the answer is not present in the retrieved context,
          explicitly state that the answer was not found in the course materials.
        - You may use prior chat history only for conversational continuity
          (for example, pronouns, follow-up questions, translations,
          formatting requests, or references to previous answers).
        - Do not treat prior assistant messages as authoritative sources.
        - Prefer concise and accurate answers over speculation.
        - Do not fabricate citations.

        When information comes from retrieved context,
        cite the supporting chunk inline.

        Citation format:

        {{1}}
        {{2}}
        {{3}}

        The number corresponds to the retrieved chunk number.

        Example:

        "Polymorphism allows multiple implementations behind a common interface {{2}}."

        Do not explain the citation format. Emit only citation markers.
        """;

    public string ContextPrompt { get; set; } =
        """
        The following chunks were retrieved from the knowledge-base.

        These chunks are the authoritative source material for this response.

        When using information from a chunk,
        cite it using the required citation format.
        """;

    public string NoContextRetrievedPrompt { get; set; } =
        """
        No relevant material was retrieved from the knowledge base.

        Unless the user is asking about prior conversation history,
        state that the answer was not found in the course materials.

        Do not speculate.
        """;

    public float CitationExtractionTemperature { get; set; } = 0.0F;

    public string CitationExtractionPrompt { get; set; } =
        """
        You are validating citations used in an answer.

        Input contains:

        1. An ANSWER.
        2. Retrieved CHUNKS.

        Citation markers appear inside the answer:

        {{1}}
        {{2}}
        {{3}}

        The number corresponds to the retrieved chunk's index:

        {{1}} -> [Chunk 1]
        {{2}} -> [Chunk 2]
        {{3}} -> [Chunk 3]

        The order of occurrence of citation markers
        is NOT related to chunk order in any away.

        For every citation marker occurrence:

        * Locate the claim immediately supported by that citation.
        * Find a supporting quote from the corresponding chunk.
        * Extract the smallest self-contained quote that supports the claim.
        * Copy quoted text verbatim from the chunk.
        * Do not modify lettercase or spacing in the quote.
        * You may use "[...]" to omit irrelevant text.
        * Do not invent text.
        * Do not paraphrase text.
        * If no supporting quote exists, use null.

        Return ONLY RFC8259-compliant JSON.

        Output format:

        [
          {
            "occurrenceId": 1,
            "chunkIndex": 2,
            "supportingQuote": "...",
            "valid": true
          }
        ]

        Rules:

        * Output one JSON object per citation marker occurrence.
        * Output count MUST equal citation marker occurrence count.
        * Preserve citation marker occurrence order.
        * The number in the citation marker MUST match the chunk index
          in the corresponding output JSON object.
        * If a citation is unsupported, set:
          {
            "supportingQuote": null,
            "valid": false
          }
        * Do not include markdown.
        * Do not include explanations.
        * Output JSON only.
        """;

    const string Example =
        """
        Example:

        Input:

          ANSWER:
          Designing a serious game requires careful consideration of several key factors,
          including:
          1. **Satisfying psychological needs**:
             Prioritize competence, autonomy, and relatedness
             to align with Self-Determination Theory {{1}}.
          2. **Meaningful choice**:
             Design decisions that resonate with players’ personalities,
             ensuring awareness of consequences, permanence,
             and alignment with gameplay mechanics {{2}} {{4}}.
          3. **Player variability**:
             Account for diverse player approaches
             by designing adaptable systems that accommodate different interaction styles {{2}}.

          CHUNKS:
          [Chunk 1]
          Competence, autonomy, and relatedness can be satisfied in various ways:
          It is suggested that the needs can be satisfied by:
          1. The design of a Zone of Proximal Development (competence).
          2. Presenting players with Progressive Feedback (competence).
          3. Design for Parallel play (relatedness).
          4. Create a social interdependency amongst players (relatedness).
          5. Offer ways to share gameplay with others (relatedness).
          6. Offer a particular amount of restructuring practices (autonomy).
          This list is not exhaustive,
          but came into being by relating insights from developmental psychology,
          with game theory, and theories on human motivation (Self-Determination Theory).

          [Chunk 2]
          Every player has a particular way of playing the game.
          Designing for these different player types 
          can be an interesting approach towards meaningful choice,
          since the choice may resonate with players’ personalities.
          Game Designer VandeBerghe (2012) consulted the audience 
          at the annual Game Developers’ Conference in San Francisco
          on how to design with particular psychological types in mind.
          VandeBerghe suggests that meaningful choice can be designed
          by looking at players, instead of the context of the game.


          [Chunk 3]
          When the learning concept is defined
          it may still be unclear which knowledge the player should construct.
          The intended knowledge construction can be narrowed down
          by clearly describing the learning goal.
          The learning goal presents designers with clear boundaries to work in.

          [Chunk 4]
          According to Morrison, players should be aware that they are making a choice,
          that this choice has consequences that are both gameplay
          and aesthetically oriented,
          that players are reminded of the choice they made,
          and that players cannot go back and undo their choice
          after exploring the consequences.
          In addition, Meier explains that every game is played in a different way.
          Every player has a particular way of playing the game.
          Designing for these different player types can be an interesting approach
          towards meaningful choice, since the choice may resonate

        Output:
        
        [
          {
            "citationIndex": 1,
            "chunkIndex": 1,
            "supportingQuote": "Competence, autonomy, and relatedness can be satisfied in various ways [...] by relating insights from developmental psychology, with game theory, and theories on human motivation (Self-Determination Theory).",
            "explanation": "Demonstrates the relevance of psychological needs and credibility of suggested methods to satisfy such needs.",
          },
          {
            "citationIndex": 2,
            "chunkIndex": 2,
            "supportingQuote": "Designing for these different player types can be an interesting approach towards meaningful choice, since the choice may resonate with players’ personalities.",
            "explanation": "Explains how personalizing game mechanics can lead to a greater sense of choice and consequences.",
          },
          {
            "citationIndex": 3,
            "chunkIndex": 4,
            "supportingQuote": "players should be aware that they are making a choice, that this choice has consequences that are both gameplay and aesthetically oriented, that players are reminded of the choice they made, and that players cannot go back and undo their choice after exploring the consequences.",
            "explanation": "Clarifies what constitutes a player's sense of choice and permanance."
          },
          {
            "citationIndex": 4,
            "chunkIndex": 2,
            "supportingQuote": "Every player has a particular way of playing the game.",
            "explanation": "Emphasizes the uniqueness and individuality of players."
          }
        ]

        Note: In the preceding example
        - The input answer text contains 4 citation markers.
          The output array has a length of 4.
          This equality MUST ALWAYS be true.
        - Chunk 3 was not referenced in the input answer text,
          thus it was not part of the output JSON array.
        """;

    public int MaxContextChunks { get; set; } = 8;

    public int MaxHistoryMessages { get; set; } = 12;
}
