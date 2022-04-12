# MicaPad

MicaPad is a UWP Notepad alternative with some additional features, like Rich Text support and the beautiful Mica backdrop (which inspired the name). Still in early stages.

## Download

You can download the latest release [here](https://github.com/LeddaZ/MicaPad/releases/latest). You'll also need to install [this](https://github.com/LeddaZ/MicaPad/blob/master/MicaPadCertificate.cer?raw=true) certificate if you're installing the app for the first time; install it to local machine and choose `Trusted Root Certification Authorities` (you'll need admin rights).

## Building

To build the app from source code, you'll need Visual Studio 2022 with the UWP workload and Windows 11 SDK installed. Clone the repository with `git clone https://github.com/LeddaZ/MicaPad` if you have git installed or click [here](https://github.com/LeddaZ/MicaPad/archive/refs/heads/master.zip) to download the source code as zip, then open the solution in Visual Studio.

## Credits

The update checking code has been implemented with [Octokit](https://github.com/octokit/octokit.net).

Of course, a good portion of the code has been adapted from [Stack Overflow](https://stackoverflow.com/) and the [XAML Controls Gallery](https://www.microsoft.com/en-us/p/xaml-controls-gallery/9msvh128x2zt) app.

## TODO

- Fix the unsaved changes dialog appearing when it shouldn't
- Maybe more

Features like images and text color are not planned at the moment, since this is intended as a "supercharged" Notepad; I'm not aiming to make a full WordPad replacement.
