from typing import Optional
import cv2
from cv2 import Mat
import numpy as np


class morphology_solver():
    def __init__(self, low_mask_thresh=10, threshold=70) -> None:
        self.reference_frame: Optional[Mat] = None
        self.mask_thresh = low_mask_thresh
        self.min_thresh = threshold
        self._unprocessed_ref_frame = self.reference_frame

    def _display(self, window_name: str, frame, debug=True):
        if debug:
            cv2.imshow(window_name, frame)

    def _convert_to_greyscale(self, frame, debug=True):
        frame_processed = cv2.cvtColor(frame, cv2.COLOR_BGR2GRAY)
        self._display("greyscale", frame_processed, debug)
        return frame_processed

    def _blur_frame_gaussian(self, frame, debug=True):
        kernel_size = (25, 25)
        processed = cv2.GaussianBlur(frame, kernel_size, 0)
        self._display("gaussian_blur", processed, debug)
        return processed

    def _blur_frame_median(self, frame, debug=True):
        ksize = 7
        processed = cv2.medianBlur(frame, ksize)
        self._display("median_blur", processed, debug)
        return processed

    def _preprocess(self, frame, debug=True):
        greyscale = self._convert_to_greyscale(frame, debug)
        blurred = self._blur_frame_median(greyscale, debug)
        blurred2 = self._blur_frame_gaussian(greyscale, debug)
        return blurred2

    def _background_subtraction(self, initial_frame, frame, debug=True):
        processed = cv2.absdiff(initial_frame, frame)
        self._display("background subtraction", processed, debug)
        return processed

    def _apply_mask_low(self, frame, debug=True):
        mask = cv2.inRange(frame, np.asarray(
            [self.mask_thresh]), np.asarray([255]))
        masked_frame = cv2.bitwise_and(frame, frame.copy(), mask=mask)
        self._display("Masked", masked_frame, debug)
        return masked_frame

    def _normalize_frame(self, frame, debug=True):
        normalized = cv2.normalize(frame, np.zeros(
            frame.shape), 0, 255, cv2.NORM_MINMAX)
        self._display("Normalized", normalized, debug)
        return normalized

    def _apply_thresholding(self, frame, debug=True):
        thresh_frame = cv2.threshold(
            frame, self.min_thresh, 255, cv2.THRESH_BINARY)[1]
        self._display("threshold frame", thresh_frame, debug)
        return thresh_frame

    def _fill_and_smooth_internal_holes(self, frame, debug=True):
        morph_frame = frame
        kernel = np.ones((7, 7), np.uint8)
        morph_frame = cv2.morphologyEx(
            morph_frame, cv2.MORPH_CLOSE, kernel, iterations=2)

        # morph_frame = cv2.morphologyEx(
        #     morph_frame, cv2.MORPH_ERODE, kernel, iterations=10)
        self._display(f"Morph", morph_frame, debug)
        return morph_frame

    def _process(self, reference_frame, frame, debug=True):
        background_subtracted_frame = self._background_subtraction(
            reference_frame, frame, debug)
        masked = self._apply_mask_low(background_subtracted_frame, debug)
        normalized = self._normalize_frame(masked, debug)
        binary_thresholded_frame = self._apply_thresholding(normalized, debug)
        smoothed = self._fill_and_smooth_internal_holes(
            binary_thresholded_frame, debug)
        return smoothed

    def solve(self, current_frame, debug=True):
        preprocessed = self._preprocess(current_frame, debug=debug)

        if self.reference_frame is None:
            self._unprocessed_ref_frame = current_frame
            self.reference_frame = preprocessed

        self._display("Reference frame", self._unprocessed_ref_frame, debug)
        foreground = self._process(self.reference_frame, preprocessed, debug)
        self._display("Final Foregound", foreground, debug)
        return cv2.findContours(foreground,
                                cv2.RETR_EXTERNAL, cv2.CHAIN_APPROX_SIMPLE)

    def update_reference_frame(self, frame):
        self.reference_frame = frame
