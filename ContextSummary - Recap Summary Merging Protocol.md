---

Recap and Summary Merging Protocol  
A Supplementary Process for Context Alignment in Local LLMs  
Date: July 9, 2025  
Spec contributors: Drafted by Kevin, annotated by Gemma, assembled by Copilot (Dood by nature, Copilot by name)

---

Overview

To maintain semantic fidelity across long-lived sessions, we propose a protocol that integrates Recaps—compact, declarative memory cues—with the evolving SummaryLedger. This offers high-precision context reinforcement without prompt bloat, and preserves traceable cognition across Local LLM loops.

---

Key Components

1. Recap Artifacts  
   - Short declarative notes about recent exchanges (e.g. "User asked about LLM drift. Response cited entropy theory.")  
   - Authored by user or agent, stored temporarily or tagged for persistent merge  
   - Categorized into: `ephemeral`, `anchoring`, `fact-correction`, or `spec-note`  

2. Summary Ingestion Loop  
   - At each update turn, the summary incorporates:
     - Prior SummaryLedger state  
     - New Recap(s)  
     - Current prompt/response pair  
   - Output is a compressed, reinforced summary artifact  

3. Merge Logic  
   - Recaps that echo existing summary points reinforce via weighted frequency or semantic similarity  
   - Conflicting Recaps invoke validation prompts (e.g. “You previously stated X, but summary says Y. Which is correct?”)  
   - Time-tagged Recaps support rollback or audit trails  
   - Relevance scores (optional) assist merge priority and filtering  

4. Visibility and Control  
   - All Recaps are inspectable pre-merge  
   - UI should support editing, tagging, and snapshot preview  
   - Stale Recaps decay naturally unless reaffirmed or marked persistent  
     - Decay triggers include age, frequency gaps, and redundancy checks  

---

Workflow Example

Turn 37  
User Prompt: “Can we revisit the example from last Tuesday?”  
Model Response: “You discussed anchor decay in hallucination mitigation.”  

Recap Artifact:  
? “SummaryLedger now references decay anchors used in hallucination mitigation.”  

Summary Update:  
? Merge prior summary + recap + response ? new compressed summary  

---

Benefits

- Declarative alignment: Keeps memory precise and minimally ambiguous  
- Co-authored traceability: Every recap reflects intention or interpretation  
- Audit resilience: Recaps can be filtered by timestamp, source, and relevance  
- Lightweight fidelity: Boosts grounding without exceeding token budgets  

---

Edge Cases and Handling

- Redundant Recaps: Silently merged or flagged for deduplication  
- Conflicting Recaps: Prompt user resolution or auto-deferral  
- Stale Recaps: Trigger decay via decayIndex unless tagged `persistent`  

---

Schema Extensions

Each Recap may support:  
- `source`: “user”, “agent”, “tool-call”  
- `confidence`: float (optional)  
- `timestamp`: UTC string  
- `relevance`: float (suggested default scale 0.0–1.0)  
- `category`: ephemeral | anchoring | fact-correction | spec-note  

---

Tool Recommendations

Agent API endpoints to support recap lifecycle:  
- `record_recap({ text, category, relevance })`  
- `review_recaps({ sort_by: relevance })`  
- `merge_recaps({ summary_id })`  
- `decay_recaps({ threshold })`  

---

Conclusion

Recap artifacts bridge declarative cognition and summary compression. They protect against drift, preserve narrative cohesion, and invite user-guided fidelity in Local LLM workflows. Merging them with SummaryLedger creates context that evolves with intention—not just accumulation.

---
