Per accedere a keycloack (https://localhost:8080) è necessario recuperare dalle variabili di ambiente del POD il valore "KC_BOOTSTRAP_ADMIN_PASSWORD" e "KC_BOOTSTRAP_ADMIN_USERNAME" per poter loggare come amministratore.

Usato SkiaSharp per licenza MIT per evitare problemi di licenza con ImageSharp.




Possibili aggiunte future:
- Aggiungere la possibilità di donare alla libreria tramite Stripe (gateway di pagamento) e tenere una percentuale per il funzionamento del sito (sostenibilità finanziaria progetto).
-

## Pannello di amministrazione

Lato server l'autorizzazione e' la policy `Admin` (vedi `AuthPolicies.Admin`), che richiede il
ruolo di realm `admin`. I ruoli arrivano nel claim `realm_access` e vengono appiattiti su
`ClaimTypes.Role` nell'evento `OnTokenValidated` in `ProgramExtensions.AddAuth`.

## Consenso al tracciamento

L'identificatore pseudonimo `x-signature` (header `X-User-Signature`, usato per deduplicare le
visite) viene generato solo dopo il consenso esplicito raccolto dal banner. Senza consenso
`useApi` non invia l'header e il backend semplicemente non conta la visita.
