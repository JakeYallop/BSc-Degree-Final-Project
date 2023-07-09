import argparse
import base64
import io
import json
import multiprocessing
import os
from pathlib import Path
from re import L
import subprocess
import time
import traceback
from typing import Any, Iterable, Optional, Sequence, Union, cast
import zlib
import cv2
from cv2 import CAP_PROP_FRAME_HEIGHT
from cv2 import VideoCapture
from cv2 import CAP_PROP_POS_FRAMES
from cv2 import Mat
import numpy as np
import compressor
import clip_manager


def display(window_name, frame, show_frame=True):
    if (show_frame):
        cv2.imshow(window_name, frame)


def convert_to_greyscale(frame, show_frame=True):
    frame_processed = cv2.cvtColor(frame, cv2.COLOR_BGR2GRAY)
    display("greyscale", frame_processed, show_frame)
    return frame_processed


def blur_frame_gaussian(frame, show_frame=True):
    kernel_size = (25, 25)
    processed = cv2.GaussianBlur(frame, kernel_size, 0)
    display("gaussian_blur", processed, show_frame)
    return processed


def preprocess(frame, show_frame=True):
    greyscale = convert_to_greyscale(frame, show_frame)
    blurred = blur_frame_gaussian(greyscale, show_frame)
    return blurred


def background_subtraction(initial_frame, frame, show_frame=True):
    processed = cv2.absdiff(initial_frame, frame)
    display("background subtraction", processed, show_frame)
    return processed


mask_thresh = 5


def apply_mask(frame, show_frame=True):
    mask = cv2.inRange(frame, np.asarray([mask_thresh]), np.asarray([255]))
    masked_frame = cv2.bitwise_and(frame, frame.copy(), mask=mask)
    display("Masked", frame, show_frame)
    return masked_frame


def normalize_frame(frame, show_frame=True):
    normalized = cv2.normalize(frame, np.zeros(
        frame.shape), 0, 255, cv2.NORM_MINMAX)  # type: ignore
    display("Normalized", normalized, show_frame)
    return normalized


min_thresh = 40


def apply_thresholding(frame, show_frame=True):
    thresh_frame = cv2.threshold(frame, min_thresh, 255, cv2.THRESH_BINARY)[1]
    return thresh_frame


def fill_and_smooth_internal_holes(frame, show_frame=True):
    kernel = np.ones((7, 7), np.uint8)
    morph_frame = cv2.morphologyEx(frame, cv2.MORPH_CLOSE, kernel)
    display("Morph", morph_frame, show_frame)
    return morph_frame


def process(initial_frame, frame, show_frame=True):
    background_subtracted_frame = background_subtraction(
        initial_frame, frame, show_frame=False)
    masked = apply_mask(background_subtracted_frame, show_frame)
    normalized = normalize_frame(masked, show_frame)
    binary_thresholded_frame = apply_thresholding(normalized, show_frame)
    smoothed = fill_and_smooth_internal_holes(
        binary_thresholded_frame, show_frame)
    return smoothed


def get_output_size(capture: VideoCapture):  # type: ignore
    return (int(capture.get(cv2.CAP_PROP_FRAME_WIDTH)),
            int(capture.get(CAP_PROP_FRAME_HEIGHT)))


def get_predicted_fps(capture: VideoCapture):  # type: ignore
    file_fps = capture.get(cv2.CAP_PROP_FPS)
    if file_fps != 0:
        return file_fps

    NUMBER_OF_FRAMES = 360
    start = time.time()
    for i in range(0, NUMBER_OF_FRAMES):
        capture.read()
    seconds = time.time() - start
    capture.set(CAP_PROP_POS_FRAMES, 0)
    return NUMBER_OF_FRAMES / seconds


def is_key(input, key):
    return input & 0xFF == ord(key)


RUN_AT_FRAMERATE = False


def capture(capture: VideoCapture, queue: multiprocessing.Queue):  # type: ignore
    MIN_RELATIVE_CONTOUR_AREA = 1.0 / 100
    MIN_FRAME_RESET_CONTOUR_AREA = 0.1 / 100

    def is_valid_contour(contour, bounding_rect, capture_area):
        (x, y, w, h) = bounding_rect
        area = cv2.contourArea(contour)
        relative_area = (area / capture_area)
        if relative_area < MIN_RELATIVE_CONTOUR_AREA:
            return False
        return True
        # aspect = w / h
        # # cars are longer than they are tall
        # # we can use the aspect ratio to check if we are tracking a person or a vehicle
        # # When the vehicle turns up the junction, the bounding rect for contour is much taller than it is longer
        # return (aspect > 1.2 and aspect < 3) or (aspect < 1 and aspect > 0.4)

    output_size = get_output_size(video)
    fps = get_predicted_fps(video)

    width, height = output_size
    max_width = 640
    aspect = width/height
    max_height = max_width//aspect
    processing_size = (int(max_width), int(max_height))

    capture_area = processing_size[0] * processing_size[1]
    queue.put((processing_size, fps))

    # fourcc = cv2.VideoWriter.fourcc(*'XVID')
    # out = cv2.VideoWriter(str("resized.avi"), fourcc, 25, processing_size)
    # out2 = cv2.VideoWriter(str("full.avi"), fourcc, 25, output_size)

    try:
        reference_frame = None
        frame_count = 0
        frames_since_last_reset = 0
        fps_ms = int(1000//fps)
        while True:
            frame_start = time.time()
            current_frame: Mat
            read_frame, raw_frame = capture.read()
            if read_frame == True:

                # out2.write(raw_frame)

                current_frame = cv2.resize(
                    raw_frame.copy(), (processing_size[0], processing_size[1]))
                # out.write(current_frame)
                frame_count += 1

                # show the original video frame
                display("original", current_frame, False)

                roi_x = 0
                roi_y = 0
                region_of_interest = current_frame[roi_y:, roi_x:]

                # preprocess frame
                preprocessed_frame = preprocess(region_of_interest, False)
                if reference_frame is None:
                    # reference_frame = current_frame
                    reference_frame = preprocessed_frame

                processed_frame = process(
                    reference_frame, preprocessed_frame, False)

                # # https://docs.opencv.org/3.4/d4/dee/tutorial_optical_flow.html
                # hsv = np.zeros_like(current_frame)
                # flow = cv2.optflow.calcOpticalFlowDenseRLOF(
                #     reference_frame, current_frame, None)
                # hsv[..., 1] = 255
                # mag, ang = cv2.cartToPolar(flow[..., 0], flow[..., 1])
                # hsv[..., 0] = ang*180/np.pi/2
                # hsv[..., 2] = cv2.normalize(mag, None, 0, 255, cv2.NORM_MINMAX)
                # processed_frame = cv2.cvtColor(hsv, cv2.COLOR_HSV2BGR)
                # display("Optical flow", processed_frame)
                # processed_frame = cv2.cvtColor(hsv, cv2.COLOR_BGR2GRAY)
                # end

                (contours, _) = cv2.findContours(processed_frame,
                                                 cv2.RETR_EXTERNAL, cv2.CHAIN_APPROX_SIMPLE)
                matched_contours = []
                has_large_contours = False
                for contour in contours:
                    bounding_rect = cv2.boundingRect(contour)
                    area = cv2.contourArea(contour)
                    if not is_valid_contour(contour, bounding_rect, capture_area):
                        continue

                    has_large_contours = True

                    (x, y, w, h) = cast(
                        tuple[int, int, int, int], bounding_rect)

                    relative_area = (area / capture_area) * 100

                    # re-adjust the coordinates so they appear in the correct
                    # place on the original frame
                    adjusted_y = y + roi_y
                    adjusted_x = x + roi_x

                    matched_contours.append(
                        ((adjusted_x, adjusted_y, w, h), contour, relative_area))

                if has_large_contours:
                    frames_since_last_reset = 0
                else:
                    if frames_since_last_reset >= 600:
                        frames_since_last_reset = 0
                        reference_frame = current_frame
                        print("Updated reference frame")
                    else:
                        frames_since_last_reset += 1

                contours = []
                if len(matched_contours) != 0:
                    contours = [(box, contour)
                                for (box, contour, _) in matched_contours]

                queue.put((current_frame.copy(), contours))

                for (box, contour, area) in matched_contours:
                    (x, y, w, h) = box
                    cv2.putText(current_frame, f"S: {area:.6}", (x - 20, y - 20),
                                cv2.FONT_HERSHEY_SIMPLEX, 1.1, (255, 255, 255), 4, 2)
                    cv2.rectangle(current_frame, (x, y),  # type: ignore
                                  (x + w, y + h), (0, 255, 0), 1)

                display(
                    "contours", current_frame)

                frame_end = time.time()
                duration = int((frame_end - frame_start) * 1000)
                wait = fps_ms - duration
                key = None
                if wait > 0 and RUN_AT_FRAMERATE:
                    key = cv2.waitKey(wait)
                else:
                    key = cv2.waitKey(1)
                if is_key(key, "q"):
                    # exit the application
                    break
            else:
                break
    except Exception:
        print(traceback.format_exc())

    print("ending subprocesses")
    queue.put(True)
    capture.release()
    cv2.destroyAllWindows()


if __name__ == "__main__":

    import sys
    parser = argparse.ArgumentParser(description="camera detection")
    parser.add_argument("-d", "--device", type=int,
                        help="camera device")
    parser.add_argument("filepath", type=str,
                        help="file path")
    args = parser.parse_args(sys.argv[1:])

    filepath = args.filepath
    device = args.device

    if filepath is None and device is None:
        print("Error: file path or device must be specified")
    elif filepath is not None and device is not None:
        print("Error: Only one of filepath or device can be specified")

    deviceOrPath = ""
    if filepath is not None:
        deviceOrPath = filepath
    if device is not None:
        deviceOrPath = device

    video = cv2.VideoCapture(
        deviceOrPath)

    if not video.isOpened():
        print("Could not open video device or file.")
        exit(1)

    queue = multiprocessing.Queue()
    clip_manager.start_processing(queue, Path("./clips"))
    capture(video, queue)
