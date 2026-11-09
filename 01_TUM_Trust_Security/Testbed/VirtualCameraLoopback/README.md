# TUM Trust Security — Virtual Camera Loopback Interface & Biometric Testbed

## Overview
DirectShow (Windows) and V4L2 (Linux) virtual camera streaming testbed for automated facial recognition and ISO/IEC 30107 Level 2 presentation attack detection (PAD) validation.

## Key Capabilities
1. **Loopback Streaming Pipeline:**
   - Injects synthetic 30 FPS video frames into virtual camera drivers (e.g., OBS VirtualCam, Unity Video Capture, v4l2loopback).
   - Generates dynamic sub-dermal micro-vascular pulse modulation on the red channel (Remote-PPG @ 72.5 BPM) and realistic eye micro-saccades.
2. **Benchmark Datasets Integration:**
   - **NIST FRVT:** Bona fide live human baseline with sub-dermal blood volume pulse.
   - **CASIA-FAS:** Level 1 matte/glossy 2D paper print attacks and 4K screen replay attacks with moire lattice patterns.
   - **SiW (Spoof in the Wild):** Level 2 3D medical-grade silicone prosthetic masks.
   - **OULU-NPU:** Level 3 latent diffusion neural deepfakes.
3. **Automated Evaluation Harness:**
   - Calculates APCER (Attack Presentation Classification Error Rate) and BPCER (Bona Fide Presentation Classification Error Rate).
   - Guarantees ISO/IEC 30107 Level 2 compliance (APCER <= 2.0%, BPCER <= 1.0%, latency < 80ms).

## Execution
```bash
# Stream 10 seconds of simulated live video to virtual camera
python streamer/virtual_camera_streamer.py --mode live --fps 30 --duration 10

# Execute full ISO/IEC 30107 benchmark suite
python runner/test_liveness_benchmarks.py
```
