# Exploratory AirData Maneuver Mining Report

Source: `Apr-10th-2026-02-12PM-Flight-Airdata.csv`

## Data quality snapshot
- Rows: 10898
- Timing: mode 100 ms, min/max 100/100 ms, zero-dt rows 1
- Active maneuvering fraction: 0.472
- Passive/neutral fraction: 0.528
- Candidate windows detected: 194, usable windows: 167

## Candidate counts by class
- backward-dominant: 12
- climb-dominant: 22
- descent-dominant: 20
- discard/noisy window: 16
- forward-dominant: 30
- left-strafe-dominant: 13
- mixed-input window: 11
- right-strafe-dominant: 14
- yaw-left-dominant: 25
- yaw-right-dominant: 31

## Best candidates by class (top ranked)

| Class | Rows | Time (s) | Hold (s) | Peak stick % | Input peak | Full peak | Carryover | Settle (s) | Confidence |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---|
| backward-dominant | 6485-6518 | 648.5-651.8 | 3.30 | 100.0 | 6.900 | 7.101 | 0.201 | n/a | high |
| backward-dominant | 6180-6195 | 618.0-619.5 | 1.50 | 100.0 | 4.491 | 5.028 | 0.537 | n/a | high |
| backward-dominant | 741-755 | 74.1-75.5 | 1.40 | 71.0 | 2.843 | 3.202 | 0.359 | 1.2 | high |
| backward-dominant | 9063-9137 | 906.3-913.7 | 7.40 | 100.0 | 4.386 | 4.386 | 0.000 | n/a | high |
| backward-dominant | 9860-9907 | 986.0-990.7 | 4.70 | 100.0 | 6.306 | 6.306 | 0.000 | n/a | high |
| backward-dominant | 9820-9856 | 982.0-985.6 | 3.60 | 100.0 | 6.332 | 6.815 | 0.483 | n/a | high |
| backward-dominant | 4813-4841 | 481.3-484.1 | 2.80 | 100.0 | 6.141 | 6.141 | 0.000 | n/a | high |
| backward-dominant | 9493-9511 | 949.3-951.1 | 1.80 | 100.0 | 5.157 | 5.951 | 0.794 | 1.4 | high |
| backward-dominant | 7172-7176 | 717.2-717.6 | 0.40 | 78.0 | 0.220 | 0.850 | 0.630 | n/a | medium |
| backward-dominant | 6318-6319 | 631.8-631.9 | 0.10 | 32.0 | 0.000 | 0.200 | 0.200 | n/a | medium |
| climb-dominant | 4167-4186 | 416.7-418.6 | 1.90 | 100.0 | 6.000 | 6.000 | 0.000 | 1.7 | high |
| climb-dominant | 2336-2353 | 233.6-235.3 | 1.70 | 53.0 | 2.000 | 2.000 | 0.000 | 0.7 | high |
| climb-dominant | 839-849 | 83.9-84.9 | 1.00 | 47.0 | 1.500 | 1.500 | 0.000 | 0.6 | high |
| climb-dominant | 3938-3956 | 393.8-395.6 | 1.80 | 100.0 | 6.000 | 6.000 | 0.000 | 1.8 | high |
| climb-dominant | 4039-4055 | 403.9-405.5 | 1.60 | 100.0 | 6.000 | 6.000 | 0.000 | 1.8 | high |
| climb-dominant | 8778-8794 | 877.8-879.4 | 1.60 | 100.0 | 2.200 | 2.600 | 0.400 | n/a | high |
| climb-dominant | 6831-6839 | 683.1-683.9 | 0.80 | 89.0 | 3.700 | 4.400 | 0.700 | n/a | high |
| climb-dominant | 9430-9438 | 943.0-943.8 | 0.80 | 100.0 | 3.600 | 3.600 | 0.000 | n/a | high |
| climb-dominant | 9955-9963 | 995.5-996.3 | 0.80 | 100.0 | 3.600 | 4.300 | 0.700 | 1.4 | high |
| climb-dominant | 4888-4894 | 488.8-489.4 | 0.60 | 100.0 | 2.200 | 3.200 | 1.000 | 1.2 | high |
| descent-dominant | 4335-4378 | 433.5-437.8 | 4.30 | 100.0 | 6.000 | 6.000 | 0.000 | 0.6 | high |
| descent-dominant | 4278-4309 | 427.8-430.9 | 3.10 | 100.0 | 6.000 | 6.000 | 0.000 | 1.2 | high |
| descent-dominant | 9663-9681 | 966.3-968.1 | 1.80 | 100.0 | 2.600 | 2.700 | 0.100 | 0.7 | high |
| descent-dominant | 4237-4253 | 423.7-425.3 | 1.60 | 100.0 | 5.400 | 5.800 | 0.400 | 1.3 | high |
| descent-dominant | 9696-9774 | 969.6-977.4 | 7.80 | 100.0 | 2.800 | 2.800 | 0.000 | 0.1 | high |
| descent-dominant | 859-866 | 85.9-86.6 | 0.70 | 100.0 | 2.200 | 2.400 | 0.200 | 0.7 | high |
| descent-dominant | 6441-6468 | 644.1-646.8 | 2.70 | 100.0 | 2.300 | 2.300 | 0.000 | n/a | high |
| descent-dominant | 6726-6746 | 672.6-674.6 | 2.00 | 100.0 | 2.100 | 2.100 | 0.000 | 0.5 | high |
| descent-dominant | 6249-6263 | 624.9-626.3 | 1.40 | 73.0 | 2.500 | 2.500 | 0.000 | 0.7 | high |
| descent-dominant | 5977-5989 | 597.7-598.9 | 1.20 | 100.0 | 1.800 | 1.800 | 0.000 | 0.2 | high |
| discard/noisy window | 7557-7560 | 755.7-756.0 | 0.30 | 100.0 | 24.000 | 68.000 | 44.000 | n/a | low |
| discard/noisy window | 7878-7881 | 787.8-788.1 | 0.30 | 95.0 | 1.200 | 1.400 | 0.200 | n/a | low |
| discard/noisy window | 111-113 | 11.1-11.3 | 0.20 | 100.0 | 2.000 | 59.000 | 57.000 | n/a | low |
| discard/noisy window | 7380-7382 | 738.0-738.2 | 0.20 | 100.0 | 0.900 | 1.200 | 0.300 | n/a | low |
| discard/noisy window | 8892-8894 | 889.2-889.4 | 0.20 | 100.0 | 0.600 | 1.100 | 0.500 | n/a | low |
| discard/noisy window | 5433-5434 | 543.3-543.4 | 0.10 | 68.0 | 3.000 | 15.000 | 12.000 | n/a | low |
| discard/noisy window | 6070-6073 | 607.0-607.3 | 0.30 | 51.0 | 0.000 | 0.098 | 0.098 | n/a | low |
| discard/noisy window | 7809-7811 | 780.9-781.1 | 0.20 | 75.0 | 0.157 | 0.395 | 0.238 | n/a | low |
| discard/noisy window | 7886-7888 | 788.6-788.8 | 0.20 | 83.0 | 0.400 | 1.100 | 0.700 | n/a | low |
| discard/noisy window | 10287-10289 | 1028.7-1028.9 | 0.20 | 82.0 | 0.000 | 25.000 | 25.000 | n/a | low |
| forward-dominant | 2525-2635 | 252.5-263.5 | 11.00 | 100.0 | 9.158 | 9.158 | 0.000 | 1.6 | high |
| forward-dominant | 1811-1873 | 181.1-187.3 | 6.20 | 100.0 | 8.420 | 8.480 | 0.060 | 1.5 | high |
| forward-dominant | 1612-1644 | 161.2-164.4 | 3.20 | 100.0 | 5.794 | 5.935 | 0.141 | 1.2 | high |
| forward-dominant | 1684-1714 | 168.4-171.4 | 3.00 | 100.0 | 6.060 | 6.420 | 0.360 | 1.3 | high |
| forward-dominant | 6202-6225 | 620.2-622.5 | 2.30 | 100.0 | 3.921 | 4.459 | 0.538 | n/a | high |
| forward-dominant | 8751-8772 | 875.1-877.2 | 2.10 | 100.0 | 3.350 | 3.957 | 0.607 | n/a | high |
| forward-dominant | 5726-5744 | 572.6-574.4 | 1.80 | 58.0 | 0.806 | 1.029 | 0.223 | 1.4 | high |
| forward-dominant | 10373-10390 | 1037.3-1039.0 | 1.70 | 100.0 | 3.312 | 4.391 | 1.079 | 1.3 | high |
| forward-dominant | 8062-8265 | 806.2-826.5 | 20.30 | 100.0 | 7.016 | 7.016 | 0.000 | n/a | high |
| forward-dominant | 10293-10361 | 1029.3-1036.1 | 6.80 | 100.0 | 7.748 | 7.751 | 0.003 | n/a | high |
| left-strafe-dominant | 7008-7042 | 700.8-704.2 | 3.40 | 89.0 | 2.814 | 3.324 | 0.510 | 1.3 | high |
| left-strafe-dominant | 2868-2894 | 286.8-289.4 | 2.60 | 100.0 | 5.879 | 6.378 | 0.498 | 1.2 | high |
| left-strafe-dominant | 9181-9244 | 918.1-924.4 | 6.30 | 100.0 | 3.519 | 3.519 | 0.000 | n/a | high |
| left-strafe-dominant | 2927-2958 | 292.7-295.8 | 3.10 | 100.0 | 6.582 | 7.008 | 0.426 | 1.3 | high |
| left-strafe-dominant | 3010-3036 | 301.0-303.6 | 2.60 | 100.0 | 6.289 | 6.801 | 0.511 | 1.2 | high |
| left-strafe-dominant | 776-791 | 77.6-79.1 | 1.50 | 49.0 | 1.623 | 1.765 | 0.142 | n/a | high |
| left-strafe-dominant | 6131-6137 | 613.1-613.7 | 0.60 | 91.0 | 0.759 | 2.012 | 1.253 | n/a | high |
| left-strafe-dominant | 8907-8942 | 890.7-894.2 | 3.50 | 100.0 | 6.746 | 6.932 | 0.186 | n/a | high |
| left-strafe-dominant | 9456-9484 | 945.6-948.4 | 2.80 | 100.0 | 4.264 | 4.264 | 0.000 | n/a | high |
| left-strafe-dominant | 7758-7805 | 775.8-780.5 | 4.70 | 100.0 | 3.281 | 3.281 | 0.000 | n/a | medium |
| mixed-input window | 8279-8642 | 827.9-864.2 | 36.30 | 100.0 | 6.527 | 6.633 | 0.106 | n/a | medium |
| mixed-input window | 8997-9059 | 899.7-905.9 | 6.20 | 100.0 | 2.500 | 2.500 | 0.000 | n/a | medium |
| mixed-input window | 7993-8024 | 799.3-802.4 | 3.10 | 99.0 | 3.576 | 3.956 | 0.381 | n/a | medium |
| mixed-input window | 8031-8038 | 803.1-803.8 | 0.70 | 39.0 | 0.000 | 0.206 | 0.206 | n/a | medium |
| mixed-input window | 4955-5051 | 495.5-505.1 | 9.60 | 100.0 | 7.092 | 7.092 | 0.000 | 0.1 | low |
| mixed-input window | 9996-10067 | 999.6-1006.7 | 7.10 | 100.0 | 5.923 | 5.923 | 0.000 | n/a | low |
| mixed-input window | 9374-9425 | 937.4-942.5 | 5.10 | 100.0 | 5.074 | 5.074 | 0.000 | n/a | low |
| mixed-input window | 8799-8841 | 879.9-884.1 | 4.20 | 100.0 | 4.166 | 4.166 | 0.000 | n/a | low |
| mixed-input window | 8846-8888 | 884.6-888.8 | 4.20 | 100.0 | 6.139 | 6.139 | 0.000 | n/a | low |
| mixed-input window | 7336-7375 | 733.6-737.5 | 3.90 | 100.0 | 77.000 | 77.000 | 0.000 | 0.1 | low |
| right-strafe-dominant | 2769-2809 | 276.9-280.9 | 4.00 | 100.0 | 9.915 | 10.339 | 0.424 | 1.7 | high |
| right-strafe-dominant | 2090-2123 | 209.0-212.3 | 3.30 | 100.0 | 8.981 | 9.621 | 0.640 | 1.6 | high |
| right-strafe-dominant | 2209-2236 | 220.9-223.6 | 2.70 | 100.0 | 7.480 | 8.200 | 0.720 | 1.5 | high |
| right-strafe-dominant | 6590-6646 | 659.0-664.6 | 5.60 | 100.0 | 6.706 | 7.045 | 0.339 | 1.2 | high |
| right-strafe-dominant | 2154-2181 | 215.4-218.1 | 2.70 | 100.0 | 7.060 | 8.000 | 0.940 | 1.6 | high |
| right-strafe-dominant | 798-813 | 79.8-81.3 | 1.50 | 82.0 | 3.560 | 3.982 | 0.422 | 1.3 | high |
| right-strafe-dominant | 9160-9172 | 916.0-917.2 | 1.20 | 100.0 | 0.585 | 1.480 | 0.894 | n/a | high |
| right-strafe-dominant | 6335-6418 | 633.5-641.8 | 8.30 | 100.0 | 3.901 | 3.901 | 0.000 | 0.9 | high |
| right-strafe-dominant | 7400-7424 | 740.0-742.4 | 2.40 | 100.0 | 0.037 | 0.869 | 0.831 | n/a | high |
| right-strafe-dominant | 9984-9992 | 998.4-999.2 | 0.80 | 100.0 | 0.851 | 1.674 | 0.824 | n/a | high |
| yaw-left-dominant | 5825-5971 | 582.5-597.1 | 14.60 | 100.0 | 94.000 | 94.000 | 0.000 | n/a | high |
| yaw-left-dominant | 3593-3646 | 359.3-364.6 | 5.30 | 100.0 | 85.000 | 85.000 | 0.000 | 0.5 | high |
| yaw-left-dominant | 6143-6168 | 614.3-616.8 | 2.50 | 77.0 | 39.000 | 39.000 | 0.000 | 0.6 | high |
| yaw-left-dominant | 6091-6099 | 609.1-609.9 | 0.80 | 100.0 | 53.000 | 71.000 | 18.000 | 0.5 | high |
| yaw-left-dominant | 3803-3884 | 380.3-388.4 | 8.10 | 100.0 | 87.000 | 87.000 | 0.000 | 0.6 | high |
| yaw-left-dominant | 3690-3752 | 369.0-375.2 | 6.20 | 100.0 | 87.000 | 87.000 | 0.000 | 0.6 | high |
| yaw-left-dominant | 6693-6722 | 669.3-672.2 | 2.90 | 100.0 | 84.000 | 84.000 | 0.000 | n/a | high |
| yaw-left-dominant | 10236-10260 | 1023.6-1026.0 | 2.40 | 100.0 | 85.000 | 85.000 | 0.000 | n/a | high |
| yaw-left-dominant | 7449-7470 | 744.9-747.0 | 2.10 | 100.0 | 80.000 | 80.000 | 0.000 | 0.2 | high |
| yaw-left-dominant | 117-137 | 11.7-13.7 | 2.00 | 100.0 | 71.000 | 71.000 | 0.000 | 0.5 | high |
| yaw-right-dominant | 5121-5169 | 512.1-516.9 | 4.80 | 95.0 | 65.000 | 65.000 | 0.000 | 0.4 | high |
| yaw-right-dominant | 3348-3390 | 334.8-339.0 | 4.20 | 100.0 | 85.000 | 85.000 | 0.000 | 0.5 | high |
| yaw-right-dominant | 877-915 | 87.7-91.5 | 3.80 | 100.0 | 81.000 | 81.000 | 0.000 | 0.6 | high |
| yaw-right-dominant | 3442-3479 | 344.2-347.9 | 3.70 | 100.0 | 83.000 | 83.000 | 0.000 | 0.6 | high |
| yaw-right-dominant | 3293-3321 | 329.3-332.1 | 2.80 | 100.0 | 85.000 | 85.000 | 0.000 | 0.5 | high |
| yaw-right-dominant | 2373-2384 | 237.3-238.4 | 1.10 | 100.0 | 82.000 | 82.000 | 0.000 | 0.5 | high |
| yaw-right-dominant | 8044-8050 | 804.4-805.0 | 0.60 | 100.0 | 64.000 | 70.000 | 6.000 | 0.5 | high |
| yaw-right-dominant | 6910-6995 | 691.0-699.5 | 8.50 | 100.0 | 82.000 | 82.000 | 0.000 | 0.4 | high |
| yaw-right-dominant | 5269-5286 | 526.9-528.6 | 1.70 | 63.0 | 21.000 | 21.000 | 0.000 | n/a | high |
| yaw-right-dominant | 2689-2703 | 268.9-270.3 | 1.40 | 100.0 | 83.000 | 83.000 | 0.000 | 0.6 | high |

## Interpretation guardrails
- These windows are exploratory and should not be used as acceptance-grade protocol evidence.
- Mixed/discard windows indicate overlapping sticks and/or insufficient settle structure.
