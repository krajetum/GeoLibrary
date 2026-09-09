# GeoLibrary

Applicazione web per pubblicare e cercare patrimoni librari privati su base geografica.
Progetto realizzato per il Project Work del CdS Informatica per le Aziende Digitali (L-31), traccia 14.

Backend in .NET 10, frontend in Vue 3 con Vuetify, database PostgreSQL con estensione PostGIS.
L'ambiente locale (API, database, Keycloak, MinIO, Redis e frontend) è orchestrato con .NET Aspire.

## Requisiti

- .NET SDK 10
- Node.js 20 o superiore
- Docker Desktop avviato (Aspire crea i container di PostGIS, Keycloak, MinIO e Redis)

## Avvio

Alla prima esecuzione servono le dipendenze del frontend:

```bash
npm install --prefix Frontend/GeoLibrary.Frontend
```

Poi si avvia tutto dall'AppHost:

```bash
dotnet run --project GeoLibrary.AppHost
```

Il comando stampa in console l'indirizzo della dashboard di Aspire, da cui si vedono lo stato dei
servizi, i log e le tracce OpenTelemetry. Il primo avvio è più lento perché Docker deve scaricare le
immagini dei container.

Una volta che i servizi sono partiti:

| Servizio | Indirizzo |
|---|---|
| Frontend | http://localhost:5173 |
| Keycloak | http://localhost:8080 |
| Console MinIO | http://localhost:9001 (utente e password: `minioadmin`) |

Le porte dell'API e della dashboard sono assegnate da Aspire e compaiono nella dashboard stessa.
Per accedere a Keycloak basterà usare l'utente admin e la password recuperabile dalla variabile di ambiente KC_BOOTSTRAP_ADMIN_PASSWORD del container direttamente dalla dashboard.

## Utenze di prova

Il realm Keycloak viene importato già configurato e contiene tre utenti di test. Sono credenziali di
un ambiente locale usa e getta, servono solo a provare il prototipo.

| Ruolo | Username | Password |
|---|---|---|
| Amministratore | geolibrary.admin@gmail.com | Test123456@ |
| Utente | test.library1@gmail.com | Test123456@ |
| Utente | test.library2@gmail.com | Test123456@ |

L'utente amministratore ha il ruolo di realm `admin` e vede in più la dashboard di amministrazione.

## Database e dati di esempio

Lo schema è gestito con le migration di Entity Framework Core, che si trovano in
`Backend/GeoLibrary.Server.Database/Migrations`. Non c'è niente da lanciare a mano: all'avvio l'API
applica le migration mancanti e, in ambiente di sviluppo, se il database è vuoto inserisce un set
minimo di utenti, librerie e libri di esempio (`GeolibrarySeedingService`).

Nella cartella `Scripts` ci sono le insert esportate dal database dopo una sessione di prova
(utenti, librerie, libri e categorie). Sono solo un esempio del contenuto delle tabelle, utile per
leggere i dati senza avviare il progetto: non vanno eseguite e non servono a far partire
l'applicazione, perché la creazione dello schema e il popolamento iniziale li fa già Entity
Framework.

Il file CSV `TestCases/import.csv` è un file di esempio per l'importazione massiva dei libri che contiene un
codice ISBN per riga.

## Test

```bash
dotnet test Tests/GeoLibrary.Server.Tests/GeoLibrary.Server.Tests.csproj
```

Gli stessi test girano su GitHub Actions a ogni push e pull request (`.github/workflows/tests.yml`).
