# 📁 File Compression Tool 🔗

## 🚀 Overview  
The **File Compression Tool** is a Windows Forms application developed in C# that allows users to compress and decompress files and folders using two lossless compression algorithms: **Huffman Coding** and **Shannon-Fano Coding**. The application supports encryption 🔒 for compressed files, progress tracking 📊, pause/resume ⏸️/▶️ functionality, and algorithm comparison ⚖️.

## 🌟 Features
- **🔧 Compression Algorithms**: Choose between Huffman and Shannon-Fano coding for compression.
- **🗂️ File and Folder Compression**: Compress single or multiple files, or entire folders.
- **🔐 Encryption**: Secure compressed files with AES encryption using a user-provided password.
- **🧾 Decompression**: Extract all files or a single file from an archive, with support for encrypted archives.
- **📈 Progress Tracking**: Visual progress bar and percentage display for compression/decompression tasks.
- **⏸️▶️ Pause/Resume and Cancel**: Pause or cancel ongoing operations.
- **⚔️ Algorithm Comparison**: Compare the performance (size and time) of Huffman and Shannon-Fano algorithms on selected files.
- **🙌 User-Friendly Interface**: Intuitive UI with clear controls.


## **Where are these algorithms useful?**
- If a file contains many repeated symbols or patterns, Huffman and Shannon-Fano coding can compress it significantly.
- Conversely, if the data is mostly random or has little repetition, the compression results will be limited or may not reduce the size noticeably.
- These algorithms work well for data with lots of redundancy, making them suitable for text files, logs, and certain types of media with repetitive patterns(e.g., JSON).
  
*Note: 🖇 * While Huffman and Shannon-Fano coding are excellent educational tools, modern applications often employ more advanced algorithms like ZIP, LZMA, or DEFLATE for practical use.

## 🖼 Screenshots from the app

![compression UI](screenshots-/sample1.png)
![compression UI](screenshots-/sample2.png)
![compression UI](screenshots-/sample3.png)


> **Note:** This project is developed strictly for the university curriculum and educational purposes only 😊. It is not intended for commercial use or production environments.

