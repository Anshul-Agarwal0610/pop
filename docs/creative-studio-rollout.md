# PoP Creative Studio: first milestone

This milestone adds three optional poll card templates (Classic, Pulse, Spotlight) and four PoPmoji reactions. The creator previews the selected style before publishing. Mobile and web poll views render the saved style; older polls keep their existing presentation.

The API stores `CreativeTemplate` and `ReactionId` on `Polls`. Both fields are nullable and the API accepts only the published IDs. Apply `Backend/BackendAPI/Migrations/20261017_Poll_Creative_Style.sql` to the target database **before** deploying the backend change. Deploy the backend before releasing a mobile build or web creator that sends the new fields.

Build 8 (`android-v1.0.0-build8`) remains the stable Android baseline. This milestone has not been deployed or included in a new APK. Result Reveal and Poll Remix are separate stages and are not enabled here.
