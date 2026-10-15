# Expanded topic sources

Implemented locally, not deployed. Apply US151 and then `Backend/BackendAPI/Migrations/US152_SourceMetadata.sql` before deploying. US152 retains source IDs, source topic/hashtag, language, region and event start time on both the topic and its source item.

Validation: 170 backend tests pass, including ten new ingestion tests. The build retains an existing nullable warning in AuthRepository. SQL migration and authenticated live integrations still require staging verification.

## Sources and activation

| Source | Behavior | Configuration |
| --- | --- | --- |
| RSS | Enabled; 18 national, regional and world feeds; five entries/feed; four concurrent requests | `PhaseOne__ApiSourcesOnly=false`, `Ingestion__rss__Disabled=false`; optional `Rss:Feeds` replacement list |
| X | India trends followed by up to ten recent posts per trend; defaults to five trends; bounded to ten trends | `Ingestion__x__Disabled=false`, `X__BearerToken`; optional `X__Woeid`, `X__MaxTrends` |
| Instagram | Top captioned public media for a fixed list of up to ten monitored hashtags; NOT a global trending feed | `Ingestion__instagram__Disabled=false`, `Instagram__AccessToken`, `Instagram__UserId`, `Instagram__GraphVersion`, indexed `Instagram__Hashtags__0` etc. |
| Events | Ticketmaster Discovery, India, next 30 days, first 50 events; cancelled events excluded | `Ingestion__events__Disabled=false`, `Events__TicketmasterApiKey`; optional `Events__CountryCode`, `Events__HorizonDays` |
| GNews / YouTube | Existing sources continue | Existing API-key settings |

Set credentials in deployment settings or local user secrets, never in committed files. New authenticated sources default to disabled. Enabling without complete configuration reports `Misconfigured` without sending a request. No authenticated API calls have been made during development.

X needs access to **both** trends and recent search. Up to 6 requests/sweep with default limits (one trends request plus five searches), not six AI calls. Trend text without a post is not submitted for generation. Social posts are preserved as source statements, not treated as verified news. A hashtag and its posts are not restricted to India just because the trend is Indian.

Instagram uses the Facebook Login Graph API hashtag-search/top-media flow. It requires an eligible Instagram professional account, token and granted app permissions/access. Pin a Graph API version supported by your app; no version is guessed in production configuration. Meta documentation could not be retrieved during this session, so the implementation has fixture-based coverage only and needs an authenticated staging check for the selected API version, permissions and fields. Keep the hashtag list stable and verify the account's rolling unique-hashtag limit before changing it. Hashtags are not a geographic filter; their posts may be multilingual or outside India.

Ticketmaster supports country filtering, but actual India inventory has not been verified. An empty successful response is reported as empty inventory, not fabricated events. This is a bounded first-page intake, not exhaustive event discovery. Event start time is stored separately and never substituted for an article publication date.

## RSS coverage

Retained: Google News India, The Hindu, Times of India, NDTV, Hindustan Times, Economic Times, India Today and BBC World.

Added publisher-listed ABP feeds:

- English state news.
- Hindi state news, Bihar and Rajasthan.
- Bengali district news and Kolkata.
- Marathi general news and agriculture.
- Tamil general news and Chennai.

Regional scripts are stored unchanged with language/region metadata. Polls remain English; generation must quote evidence in its original language. Cross-language semantic deduplication is not implemented. Source text is limited to feed-provided titles/descriptions; no article scraping or AI is used in ingestion.

Each feed can be replaced or disabled through `Rss:Feeds` entries with `Name`, `Url`, `Category`, `Language`, `Region`, and `Enabled`. Feed endpoints are operator configuration, not user-entered URLs. Items are interleaved across feeds and then across source families, reducing starvation from one large feed. Failed feeds do not discard successful feeds; source health reports partial failures. RSS rejects XML external entities and skips stale/future-dated articles.

Public RSS probes on 2026-09-27 returned HTTP 200 and XML items for the 16 direct publisher endpoints (six retained national publishers plus ten ABP endpoints). This validates endpoint availability, not continuous freshness or deployment connectivity. Google News and BBC were retained separately. Unit tests cover the application's RSS parser.

Indian Express has regional RSS feeds, but its published RSS terms restrict use to personal/non-commercial consumption; it was not added to defaults. Broader coverage in other languages can be added through the same registry once endpoints are selected.

## Checks before enabling authenticated sources

Use `POST /api/admin/pipeline/ingestion/run` with `source` equal to `rss`, `x`, `instagram`, or `events` and a small `maxTopics`. The existing admin authorization policy still applies. Inspect `GET /api/admin/pipeline/health`, saved source metadata, and Hangfire logs. Verify duplicate intake creates no additional source items. Check one topic through generation/publication and confirm source links in the app.

The new HTTP client omits request logging because Ticketmaster requires its key in the query string. Provider response bodies are not logged. Requests stop for the current sweep after access denial or rate limiting. There is no cross-worker persisted provider cooldown in this change; avoid repeated manual runs against a rate-limited source.

## Provider references

- [X trends by WOEID](https://docs.x.com/x-api/trends/trends-by-woeid/introduction)
- [X recent post search](https://docs.x.com/x-api/posts/search-recent-posts)
- [Ticketmaster Discovery API](https://developer.ticketmaster.com/products-and-docs/apis/discovery-api/v2/)
- [Meta hashtag-search documentation (requires verification in your account)](https://developers.facebook.com/docs/instagram-platform/instagram-api-with-facebook-login/hashtag-search)
- [ABP English RSS](https://news.abplive.com/rss), [Hindi](https://www.abplive.com/rss), [Bengali](https://bengali.abplive.com/rss), [Marathi](https://marathi.abplive.com/rss), [Tamil](https://tamil.abplive.com/rss)
- [Indian Express RSS and terms](https://indianexpress.com/rss/)
