# BeamNG LogiWheel LEDs

RPM/shift lights for Logitech wheels (G923, G29, G27...) in BeamNG.drive, along with some extras.
It reads BeamNG's OutGauge UDP telemetry and drives the wheel LEDs through the Logitech Steering Wheel SDK.
Built because FanaLEDs won't fucking work.

> Not affiliated with or endorsed by Logitech or BeamNG. Provided as-is, no warranty.

## Features
- **LED modes, switchable live** (dropdown or `Ctrl+Alt+M` in-game):
  Progressive, Race (progressive + flash near the limit), Shift-light only.
- **Gauge modes:** fuel level, engine or oil temperature and turbo boost.
- **Janky reverse gear fix** for worn Logitech shifters (reverse read as the 6th-gear button), with a neutral-on-release key.
- Auto or manual redline, device selector, settings saved to `BeamLeds.ini`, tray icon.
- The G920 isn't supported by this program because it doesn't even have LEDs lol.

## Requirements
- Windows, Logitech G HUB (or Logitech Gaming Software for older wheels)
- The **Logitech Steering Wheel SDK** (not included - read below)

## Build
1. Download the Logitech Steering Wheel SDK (v8.75.30) from Logitech's [Partner Developer Lab](https://www.logitechg.com/en-us/programs/partner-developer-lab)
   (direct link: <https://www.logitechg.com/sdk/LogitechSteeringWheelSDK_8.75.30.zip>).
   There is also an unofficial third-party copy of the same version on [GitLab](https://gitlab.com/modanhan/logitech-wheel-sdk/-/tree/master) - prefer Logitech's own download.
2. Copy `Lib\GameEnginesWrapper\x64\LogitechSteeringWheelEnginesWrapper.dll` next to `BeamLeds.cs`.
3. Run `build.bat` (uses the C# compiler that ships with Windows). It creates `BeamLeds.exe`.

> **Why isn't the DLL in this repo?** The SDK's license grants a limited, non-transferable right to *use* it and reserves all other rights to Logitech - therfore, no redistribution. So your lazyass can download it yourself.

## Use
1. BeamNG: *Options > Other > Protocols > OutGauge UDP protocol* - enable, IP `127.0.0.1`, port `4444` (or whatever you currently have it set in beamng).
2. Run `BeamLeds.exe`. Cilck **Simulate RPM sweep** to test the LEDs without the game.
3. OutGauge doesn't send max RPM: untick *Auto* and type your redline, or keep *Auto* (learns the highest RPM seen; use *Reset learned peak* when changing cars).

### Reverse fix

A worn reverse and a real 6th often send the same button, so by default the app uses speed: button pressed + slower than N km/h => it taps a **Reverse key**; when you leave R it taps a **Neutral key** (Might not always work on automatic or DCT cars).
1. In BeamNG *Options > Controls*, bind two unused keys (F13-F24 / Numpad) to reverse and neutral.
2. In the app: Enable, **Learn** the button (push into R within 8 s), pick the same two keys.
3. If R and 6th are different buttons on your shifter, tick *Real 6th gear is a different button* and Learn it too.

If BeamNG runs as Administrator, run `BeamLeds.exe` as Administrator as well.

## OutGauge fields used
`gear` (offset 10), `speed` (12, m/s), `rpm` (16), `turbo` (20, bar), `engTemp` (24), `fuel` (28), `oilTemp` (36).
See the [BeamNG protocols docs](https://documentation.beamng.com/modding/protocols).

## Testing 
Currently it was **only tested with a G923**, since i don't own or know someone who owns a G29 or G27, but it *should* work

## License
MIT for this repository's code (see `LICENSE`). The Logitech SDK is Logitech's and is not covered.
