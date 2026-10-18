# PoP Poll Remix

On an eligible public poll, **Remix this poll** opens the normal creator with the original question and creator shown for context. The new question starts blank and must differ from the original. The creator can select a new style, result reveal, audience, and optional media; the source poll's media, votes, and results are not copied.

The API saves `RemixedFromPollId` only for a published, public, non-wellness source. A remix detail page links back to the source when that source remains public and published. If it is removed or made unavailable, its question and creator are no longer returned in attribution.

This branch is stacked on Result Reveal PR #212, which is stacked on Creative Studio PR #211. Apply migrations in this order before deploying the combined backend: `20261017_Poll_Creative_Style.sql`, `20261018_Poll_Result_Visibility.sql`, `20261018_Poll_Remix.sql`. Deploy the backend before the updated web app or Android build. Build 8 remains the stable Android baseline; nothing from this branch has been deployed.
