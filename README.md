This project is in early development.

This repository contains IPC connectors for Resonite that send data to another process using shared memory (memory mapped file) in C#.

This is for an attempt to give the Resonite headless server software a graphical output.

The following screenshots are of the Unity client (linked below)

![Screenshot 2025-02-27 085746](https://github.com/user-attachments/assets/2e15c1c6-d263-4b96-bcc9-31b4e3edc742)

![Screenshot 2025-03-04 153057](https://github.com/user-attachments/assets/e1bc4d72-f300-48cd-b8ea-ad02c666a40f)

![Screenshot 2025-03-05 133913](https://github.com/user-attachments/assets/f9f7cb91-c2b9-4f91-8e5a-2df356dcb1b1)

---

Put the release files in `Resonite\Headless\HeadlessLibraries`

Use headless launch argument `-LoadAssembly "HeadlessLibraries\HeadlessHeadServer.dll"`

Unity client repo (Unity 6000.0.41f1): https://github.com/Nytra/ResoniteHeadlessHeadUnity
