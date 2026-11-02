"""
Generates a SIMULATED summary.csv in the exact schema SessionLogger writes, so
the analysis chain can be verified end to end before any participant is run.

This is NOT participant data. Every row carries data_source=simulated, the file
is named simulated_summary.csv, and the report states plainly that the user
study has not yet been run. The point of this is to prove the logging format,
the analysis script and the figures all work together, which is a real thing to
check and a real thing to report.

Fixed seed, so re-running reproduces the same file exactly.
"""
import numpy as np, csv

rng = np.random.default_rng(20261102)

HEADER = ["participant", "condition", "attempt", "collisions", "limit_warnings",
          "cycle_time_s", "points_taught", "path_length_deg", "task_complete",
          "session_time_s", "data_source"]

# Effect sizes deliberately modest, the size a 12 person study would plausibly
# show: a clear direction but nowhere near significance at this sample size.
PROFILE = {
    "vr_trained": dict(coll=3.1, lim=2.5, cycle=18.5, sess=245, fail=0.04),
    "sheet_only": dict(coll=5.3, lim=4.0, cycle=21.8, sess=305, fail=0.12),
}
N_PER_GROUP = 6
ATTEMPTS = [1, 2]

rows = []
pid = 1
for cond in ("vr_trained", "sheet_only"):
    p = PROFILE[cond]
    for _ in range(N_PER_GROUP):
        name = "P{:02d}".format(pid); pid += 1
        skill = rng.normal(0.0, 0.26)            # this person's own aptitude
        for att in ATTEMPTS:
            learn = 1.0 if att == 1 else 0.74    # everyone improves on the retry
            coll = max(0, int(round(rng.poisson(max(0.2, p["coll"] * learn)) * (1 + skill))))
            lim = max(0, int(round(rng.poisson(max(0.2, p["lim"] * learn)) * (1 + skill))))
            cycle = round(float(rng.normal(p["cycle"] * learn * (1 + skill * 0.5), 2.4)), 1)
            sess = int(round(rng.normal(p["sess"] * learn * (1 + skill * 0.6), 34)))
            pts = int(rng.integers(6, 10))
            path = int(round(rng.normal(620 + coll * 22, 90)))
            done = 0 if rng.random() < p["fail"] * learn else 1
            rows.append([name, cond, att, coll, lim, max(6.0, cycle), pts,
                         max(200, path), done, max(90, sess), "simulated"])

with open("simulated_summary.csv", "w", newline="") as f:
    w = csv.writer(f); w.writerow(HEADER); w.writerows(rows)

print("wrote simulated_summary.csv with", len(rows), "rows")
print("schema matches SessionLogger:",
      ",".join(HEADER[:-1]) ==
      "participant,condition,attempt,collisions,limit_warnings,cycle_time_s,"
      "points_taught,path_length_deg,task_complete,session_time_s")
