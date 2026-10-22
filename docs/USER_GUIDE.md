# BatteryLens — User Guide

BatteryLens reads the power sensors of your Windows PC and tells you, in plain language, whether the **power supply (charger)**, the **battery** or the **computer itself** needs attention, so you do not replace the wrong part.

## The pages
| Page | What it shows |
|---|---|
| **Overview** | Three verdict cards (Power supply, Battery, Computer), each KEEP, REPLACE or CALIBRATE, with the likely cost. On a laptop, the **Battery health** bar shows how much of the original capacity is left. |
| **Checks** | Every item BatteryLens verified, with its result. |
| **System** | Your hardware and a live charge graph. |
| **Simulator** | Sample batteries (worn, healthy, charger fault, gauge out of sync, …). Press **Load** to see how the verdicts change; **Back to live hardware** returns to your real data. |
| **Buying guide** | What to look for when a part really does need replacing. |

## Reading a verdict
- **KEEP** — nothing to do for this part.
- **REPLACE** — the measurements point to this part as the cause. The card lists the evidence.
- **CALIBRATE ONLY** — the battery gauge has drifted from the real charge. No part is needed; run a calibration cycle (below).

Battery health is *full-charge capacity ÷ design capacity*. Around 80 % is when most makers consider a battery worn; below about 60 % expect short runtimes.

## Calibrating the battery gauge
1. Charge to 100 % and leave the charger connected for one hour.
2. Unplug and use the laptop normally until it falls to about 5 % (save your work and keep the PC from sleeping).
3. Plug in and charge to 100 % **without interruption**.
4. Open BatteryLens again; the gauge verdict should have cleared.

## Exporting a report
Press **Export report**. BatteryLens saves a styled HTML report to your Desktop that you can send to a customer or attach to a ticket. Reports contain the verdicts, measurements and checks.


## Problems
Errors are logged to `%AppData%\BatteryLens\error.log`. Open an [issue on GitHub](https://github.com/md-anamul-hasan/batterylens/issues) and attach that file (remove anything private first).
