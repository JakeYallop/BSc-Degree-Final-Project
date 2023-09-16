# BSc Computer Science, Final Project September 2023

The source code for my final project. The project template was the Camera Motion Surveillance System.

# Running the code

## Prerequisites
* **Python** installed and available on PATH
* **.NET SDK v7.0** or greater, `dotnet` command available on PATH
* **Node v16.17** or higher
* `pnpm` - [Installation | pnpm](https://pnpm.io/installation)


## Install dependencies
Firstly, install required dependencies:

Node
```
pnpm install
```

.NET
```
dotnet restore
```

Python

Create a new virtual environment:
```
python -m venv .venv
.venv/Scripts/activate
```

Install dependencies
```
pip install -r Python/requirements.txt
```

## Generate VAPID (**V**oluntary **A**pplication **S**erver **Id**entity) keys for push notifications

```
pnpm run keys
```

You should see output similar to
```
Setting up VAPID keys...
Generating VAPID keys...
Writing public key to client...
Writing public and private keys to push notifications...
VAPID keys successfully generated and written to .env and .env.local files.
```

## Setup local settings
```
pnpm run setup-defaults
```

This step generates VAPID (**V**oluntary **A**pplication **S**erver **Id**entity) keys used for push notifications, as well as sets up environment variables with the correct urls that different services run on. If running services on ports other than the default, these may need to be tweaked. The variables can be found in .env.local and appsettings.Development.json files.

The NotificationsBaseUrl is already hardcoded in the appsettings.Development.json file, this is unaffected by the setup script.

Client (.env.local file):
* VITE_API_BASE_URL - the base URL for the Web service
* VITE_PUSH_SERVER_BASE_URL - the base URL for the PushNotifications service

Web (appsettings.Development.json file):
* NotificationsBaseUrl - the base URL for the PushNotifications service

PushNotifications (.env.local file):
* VITE_CLIENT_URL - the base URL for the Client service

Python (environment variables):
* API_BASE_URL - the base URL for the Web service - this is passed to the executing python script using an environment variable.

## Run the services

### Web
```
pnpm run web
```
Make sure to view the swagger page!
If running from Visual Studio, use the https launch profile.

### Push notifications

```
pnpm run push
```

### Client

```
pnpm run client
```

