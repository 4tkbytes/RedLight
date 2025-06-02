# this python script grabs the log from the stress test
# and graphs it. this is used to see performance

import re
import matplotlib.pyplot as plt
import os

def parse_log(filename):
    pattern = re.compile(r"Spawned object count: (\d+) \| FPS: ([\d\.]+)")
    counts = []
    fps = []
    with open(filename, "r", encoding="utf-8") as f:
        for line in f:
            match = pattern.search(line)
            if match:
                counts.append(int(match.group(1)))
                fps.append(float(match.group(2)))
    return counts, fps

def main():
    log_file = os.path.join(os.path.dirname(__file__), "../logs/log20250602.txt")
    counts, fps = parse_log(log_file)
    if not counts:
        print("No data found in log.")
        return

    plt.figure(figsize=(10, 5))
    plt.plot(counts, fps, marker='o')
    plt.title("FPS vs Spawned Object Count")
    plt.xlabel("Spawned Object Count")
    plt.ylabel("FPS")
    plt.grid(True)
    plt.tight_layout()
    plt.show()

if __name__ == "__main__":
    main()