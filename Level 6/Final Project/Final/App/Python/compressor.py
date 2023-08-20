
from concurrent.futures import thread
import datetime
from multiprocessing import Process, process
import os
from pathlib import Path
import threading
from typing import Callable, Optional
from ffmpeg import FFmpeg, Progress


def map_compression_level_to_crf(preset):
    match preset:
        case "h": return 38
        case "m": return 28
        case "l": return 22
    return 12

# "ffmpeg -i input.mp4 -c:v libx265 -crf 28 output.mp4"


class CompleteHolder():
    def __init__(self, onComplete) -> None:
        self.onComplete = onComplete


def compress(filepath, crf: Optional[int] = None):
    print("calling compress")
    path = Path(filepath).resolve()
    output_path = path.with_suffix(".mp4")
    if output_path.exists():
        output_path = output_path.with_stem(
            f"{datetime.datetime.utcnow().timestamp()}{output_path.stem}")

    compress_core(path, crf, output_path)
    return output_path


def compress_core(path: Path, crf: Optional[int], output_path: Path):
    ffmpeg = (
        FFmpeg()
        .option("y")
        .option("hide_banner")
        .input(path)
        .output(
            output_path,
            {"c:v": "libx264"},
            preset="medium",
            crf=crf if crf is not None else map_compression_level_to_crf("m"),
        )
    )

    @ffmpeg.on("start")
    def on_start(arguments: list[str]):
        print("arguments:", arguments)

    @ffmpeg.on("stderr")
    def on_stderr(line):
        print(line)

    # @ffmpeg.on("completed")
    # def on_completed():
    #     print("completed")

    ffmpeg.execute()


if __name__ == "__main__":
    import argparse
    import sys

    parser = argparse.ArgumentParser("Compress video files")
    parser.add_argument("file", type=str)
    parser.add_argument("-c", metavar="Compression level",
                        default="medium", choices=["h", "m", "l"])
    parser.add_argument("-crf", type=int)
    args = parser.parse_args(sys.argv[1:])
    path = Path(args.file)

    crf = args.crf
    if crf is None:
        crf = map_compression_level_to_crf(args.c)
    compress(args.file, crf)
