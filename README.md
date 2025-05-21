# Multimodal AI Assistant and Extended Reality (XR) Applications

## Project Description

This project explores the integration of Vision Language Model (VLM), Large Language Model (LLM), and extended reality (XR) to create a Multimodal AI assistant with voice chat, Image understanding, and smart building control in immersive environments. This project aims to create an innovative solution for remote facility management and urban infrastructure monitoring. The developed system deploys an LLM-based AI assistant and a digital building twin into an XR environment using Microsoft HoloLens 2. Users can interact with the BIM models and communicate with the Multimodal AI chatbot. Users can also interact with the AI assistant through voice commands to control building facilities. This setup enhances the ability of facility managers and occupants to interact with and control smart buildings remotely. The approach also holds the potential for scaling to multiple buildings or urban infrastructure, enabling immersive, real-time monitoring and management for smart city applications.


<img src="/fig1.png" style="float: left; margin-right: 20px; max-width: 200px;">
### AI Voice Chat and Image Understanding

## Video Demo


<table>
  <tr>
    <td style="text-align: center;">
      <a href="https://www.youtube.com/watch?v=2esRfU4-7II" target="_blank">
        <img src="https://img.youtube.com/vi/2esRfU4-7II/0.jpg" alt="Demo Video 1" style="width: 300px;">
      </a>
      <p>*Click on the image to view Demo Video 1.*</p>
    </td>
    <td style="text-align: center;">
      <a href="https://www.youtube.com/watch?v=-Nxg_IkAl_c" target="_blank">
        <img src="https://img.youtube.com/vi/-Nxg_IkAl_c/0.jpg" alt="Demo Video 2" style="width: 300px;">
      </a>
      <p>*Click on the image to view Demo Video 2.*</p>
    </td>
  </tr>
</table>

### Key Features

- **LLM-Based AI Agents:** Utilizes advanced language models for intelligent interaction and control.
- **Extended Reality (XR) Integration:** Implements XR technologies with Unity 3D to create immersive smart building control applications as well as BIM model manipulation.
- **AI Voice Chat:** Enables natural language communication with the smart building system.
- **Image Understanding:** Incorporates vision language models for understanding and interpreting visual data.

### Requirements
- Open-source Vision language model and Large Language Model (e.g., MiniCPM V, LLaMA 3)
- Generative AI inference tool. llama.cpp
- Unity 3D
- Microsoft Hololen 2
- Python 3.10
- Open-source Text-to-Speech (TTS) model, Whisper
- Open-source Speech-to-Text (STT) model, Piper



## 🛠️ Detailed Setup Guide

### Step 1: Unity XR Setup

Install and open **Unity 3D**. Use the provided `Assets` folder in the `Unity 3D` project directory of this repository.  
This setup is designed for XR devices such as the **Microsoft HoloLens 2** using **MRTK (Mixed Reality Toolkit)**.

---

### Step 2: LLM and VLM Server Setup

You need to set up the **LLM and Vision Language Model server** on a host machine (e.g., a MacBook Pro):

- **LLaMA 3**: Used for processing **user voice queries** received from HoloLens 2.
- **MiniCPM 2.6**: Used for processing **images and video** sent from the HoloLens device.

Refer to the official guide for setting up MiniCPM V server:  
👉 https://github.com/OpenBMB/MiniCPM-o

---

### Step 3: Speech-to-Text and Text-to-Speech Configuration

Configure models to enable seamless voice interaction:

- **Speech-to-Text (STT)**: Converts spoken input from HoloLens 2 into text using **Whisper**.
- **Text-to-Speech (TTS)**: Converts LLaMA 3 responses into audio using **Piper**.

These models run on the same server as the LLM and allow users to communicate naturally with the AI assistant.

---

### Step 4: Create Secure HoloLens-Server Communication

This tutorial uses **zrok** to create a secure HTTPS tunnel between the HoloLens 2 and the AI server.

> This allows real-time data (video, voice, text) to be exchanged securely and remotely.

---


