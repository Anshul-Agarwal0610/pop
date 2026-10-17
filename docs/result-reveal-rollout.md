# PoP Result Reveal

Poll creators can select live results, results after a vote (the default), or results when the poll closes. The API returns `canViewResults` and removes totals, option counts, and percentages until the viewer is eligible. Creators retain their performance view. The raw vote-list endpoint now requires the creator's authenticated account.

Apply `Backend/BackendAPI/Migrations/20261017_Poll_Creative_Style.sql` for the parent Creative Studio PR, then `Backend/BackendAPI/Migrations/20261018_Poll_Result_Visibility.sql`, **before** deploying the combined backend. Deploy the backend before the web creator or a new Android build uses the setting. Existing polls and clients default to `AfterVote`.

This branch is stacked on `codex/creative-studio` (PR #211). Build 8 remains the Android release baseline. No migration, API, frontend, or APK from this branch has been deployed.
