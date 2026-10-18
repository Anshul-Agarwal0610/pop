# PoPmoji sticker editor rollout

The mobile creator offers four illustrated PoPmoji stickers. A creator can drag the chosen sticker over the question area and choose Small, Medium, or Large. The selected position and size appear in the creator review, mobile feed, poll detail, and shared PNG card.

Placement is stored as normalized `ReactionX` and `ReactionY` values in `[0, 1]` and `ReactionScale` in `[0.7, 1.5]`. Existing polls with a `ReactionId` but no placement use the mobile default. Polls without a sticker remain unchanged. The API validates the ranges and does not allow placement without a sticker.

Apply `Backend/BackendAPI/Migrations/20261018_Poll_Reaction_Placement.sql` after the Creative Style, Result Visibility, and Remix migrations and before deploying this backend. Deploy the backend before testing mobile publication from this branch; the current mobile creator includes the new fields in create requests. Build 8 remains the stable APK until a new release build is made.
