import argparse
import multiprocessing
import os
from pathlib import Path

import cv2

import clip_manager
from capture import capture


if __name__ == "__main__":
    print("Ensure API_BASE_URL is set as an environment variable to make sure that clips are exported to the API. API_BASE_URL should be the URL to the Web project (the web API).")
    import sys
    parser = argparse.ArgumentParser(description="camera detection")
    parser.add_argument("-d", "--device", type=int,
                        help="capture device")
    parser.add_argument("-f", "--file", type=str,
                        help="Path to video file, useful for testing purposes")
    args = parser.parse_args(sys.argv[1:])

    filepath = args.file
    device = args.device

    if filepath is None and device is None:
        print("Error: file path or device must be specified")
        exit(1)
    elif filepath is not None and device is not None:
        print("Error: Only one of filepath or device can be specified")
        exit(1)

    video: cv2.VideoCapture = None  # type: ignore
    if filepath is not None:
        video = cv2.VideoCapture(str(Path(filepath).resolve()))
    if device is not None:
        video = cv2.VideoCapture(int(device))

    if not video.isOpened():
        print("Could not open video device or file.")
        exit(1)

    queue = multiprocessing.Queue()

    out_dir = Path("./clips")
    if not out_dir.exists():
        os.mkdir(out_dir)

    clip_manager.start_processing(queue, out_dir)
    capture(video, queue)
