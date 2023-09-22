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

## Running the object detector/clip creator

* Ensure that API_BASE_URL is set to the URL of the Web service. Without this set, clips will not be uploaded.
* Execute `main.py`, passing in configuration as required. Use `-h` to see possible options.

The launch.json file in this repository runs main.py with a test clip (If you are using VSCode, you see the profile in Run and Debug).

With `API_BASE_URL` set up, this command runs from the "./Python" directory.
```
main.py -f "./Entrance Camera - 4-24-2023, 4.17.54am cat.mp4"
```

## Accessing the localhost client on an android mobile device

### Prerequisites
* USB debugging must be enabled

### Allow HTTPS requests
Configure your web browser on the mobile device to allow insecure requests on localhost. On Chrome:
chrome://flags/#allow-insecure-localhost

or, install a valid certificate on the mobile device and configure the Client project to use it by editing the `mkcert` plugin.

### Port forward to device

Ensuring the mobile device is connected via USB and debugging access has been granted, port forward localhost5173 to 5173.

This is done inside the desktop web browser and will be browser specific. In Chrome: Go to chrome://inspect/#devices and click "Port forwarding".


## Troubleshooting

### Error when starting python script
```
python main.py -d 0
```

Can sometimes fail with an error:
```
[ WARN:0@1.026] global cap_msmf.cpp:471 `anonymous-namespace'::SourceReaderCB::OnReadSample videoio(MSMF): OnReadSample() is called with error status: -1072875772
[ WARN:0@1.029] global cap_msmf.cpp:483 `anonymous-namespace'::SourceReaderCB::OnReadSample videoio(MSMF): async ReadSample() call is failed with error status: -1072875772
[ WARN:1@1.033] global cap_msmf.cpp:1759 CvCapture_MSMF::grabFrame videoio(MSMF): can't grab frame. Error: -1072875772
```

This happens when another application is already using the capture device.