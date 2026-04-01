#!/usr/bin/env python3
import csv, json, math, statistics, argparse
from dataclasses import dataclass

@dataclass
class Segment:
    maneuver:str
    start:int
    end:int
    axis:str
    sign:int

AXES=[('forward','rc_elevator(percent)'),('lateral','rc_aileron(percent)'),('vertical','rc_throttle(percent)'),('yaw','rc_rudder(percent)')]


def f(row,key):
    v=row.get(key,'').strip()
    if not v:
        return 0.0
    try:return float(v)
    except:return 0.0

def load(path):
    rows=[]
    with open(path,newline='',encoding='utf-8-sig') as fp:
        r=csv.DictReader(fp)
        for raw in r:
            rows.append({
                't_ms':int(float(raw['time(millisecond)'])),
                'mode':raw.get('flycState','').strip(),
                'speed_mph':f(raw,'speed(mph)'),
                'vx_mph':f(raw,' xSpeed(mph)'),
                'vy_mph':f(raw,' ySpeed(mph)'),
                'vz_mph':f(raw,' zSpeed(mph)'),
                'zSpeed_mph':f(raw,' zSpeed(mph)'),
                'alt_ft':f(raw,'height_above_takeoff(feet)') or f(raw,'altitude(feet)'),
                'heading_deg':f(raw,' compass_heading(degrees)') if ' compass_heading(degrees)' in raw else f(raw,'compass_heading(degrees)'),
                'pitch_deg':f(raw,' pitch(degrees)'),
                'roll_deg':f(raw,' roll(degrees)'),
                'gimbal_pitch_deg':f(raw,'gimbal_pitch(degrees)'),
                'rc_elevator':f(raw,'rc_elevator(percent)'),
                'rc_aileron':f(raw,'rc_aileron(percent)'),
                'rc_throttle':f(raw,'rc_throttle(percent)'),
                'rc_rudder':f(raw,'rc_rudder(percent)')
            })
    # elapsed time
    t0=rows[0]['t_ms']
    for r in rows:
        r['t']= (r['t_ms']-t0)/1000.0
    return rows

def segment(rows,active_th=18.0,cross_th=12.0,min_len=0.8):
    usable=[r for r in rows if r['mode']=='P-GPS']
    segs=[]
    i=0
    n=len(usable)
    while i<n:
        r=usable[i]
        vals={'forward':r['rc_elevator'],'lateral':r['rc_aileron'],'vertical':r['rc_throttle'],'yaw':r['rc_rudder']}
        dom=max(vals,key=lambda k:abs(vals[k]))
        if abs(vals[dom])<active_th:
            i+=1; continue
        j=i
        sign=1 if vals[dom]>=0 else -1
        while j<n:
            rr=usable[j]
            v={'forward':rr['rc_elevator'],'lateral':rr['rc_aileron'],'vertical':rr['rc_throttle'],'yaw':rr['rc_rudder']}
            if abs(v[dom])<active_th*0.7 or (1 if v[dom]>=0 else -1)!=sign:
                break
            others=[k for k in v if k!=dom]
            if any(abs(v[k])>cross_th for k in others):
                break
            j+=1
        dur=usable[j-1]['t']-usable[i]['t'] if j>i else 0
        if dur>=min_len:
            label={('forward',1):'forward_step',('forward',-1):'backward_step',('lateral',1):'lateral_right',('lateral',-1):'lateral_left',('vertical',1):'climb',('vertical',-1):'descent',('yaw',1):'yaw_right',('yaw',-1):'yaw_left'}[(dom,sign)]
            segs.append(Segment(label,i,j-1,dom,sign))
        i=max(j,i+1)
    # merge close gaps same maneuver
    merged=[]
    for s in segs:
        if merged and merged[-1].maneuver==s.maneuver and s.start-merged[-1].end<=4:
            merged[-1].end=s.end
        else:
            merged.append(s)
    return usable, merged

def mph_to_mps(x): return x*0.44704

def compute_metrics(usable,segs):
    by={}
    for s in segs:
        by.setdefault(s.maneuver,[]).append(s)
    out={}
    dt=0.1
    for m,arr in by.items():
        runs=[]
        for s in arr:
            win=usable[s.start:s.end+1]
            t0=win[0]['t']
            rc=[abs(r[f'rc_{"elevator" if s.axis=="forward" else ("aileron" if s.axis=="lateral" else ("throttle" if s.axis=="vertical" else "rudder"))}']) for r in win]
            if s.axis=='forward': sp=[mph_to_mps(abs(r['vx_mph'])) for r in win]
            elif s.axis=='lateral': sp=[mph_to_mps(abs(r['vy_mph'])) for r in win]
            elif s.axis=='vertical': sp=[mph_to_mps(abs(r['zSpeed_mph']) if 'zSpeed_mph' in r else abs(r['vz_mph'])) for r in win]
            else:
                # heading derivative deg/s
                sp=[]
                prev=win[0]['heading_deg']
                for r in win:
                    d=((r['heading_deg']-prev+540)%360)-180
                    sp.append(d/dt)
                    prev=r['heading_deg']
                sp=[max(0,x*s.sign) for x in sp]
            peak=max(sp) if sp else 0
            if peak<=1e-5: continue
            idx10=next((i for i,v in enumerate(sp) if v>=0.1*peak),None)
            delay=(idx10*dt) if idx10 is not None else None
            accel=max((sp[i+1]-sp[i])/dt for i in range(len(sp)-1)) if len(sp)>1 else 0
            stop_idx=next((i for i in range(len(sp)-1,-1,-1) if sp[i]>=0.1*peak),0)
            settle=(len(sp)-1-stop_idx)*dt
            runs.append({'start_s':win[0]['t'],'end_s':win[-1]['t'],'duration_s':win[-1]['t']-win[0]['t'],'peak':peak,'response_delay_s':delay,'max_accel':accel,'settle_tail_s':settle,
                        'mean_pitch_deg':statistics.fmean(r['pitch_deg'] for r in win),'mean_roll_deg':statistics.fmean(r['roll_deg'] for r in win)})
        if runs:
            out[m]={
                'count':len(runs),
                'peak_mean':statistics.fmean(r['peak'] for r in runs),
                'peak_max':max(r['peak'] for r in runs),
                'delay_mean':statistics.fmean(r['response_delay_s'] for r in runs if r['response_delay_s'] is not None),
                'accel_mean':statistics.fmean(r['max_accel'] for r in runs),
                'settle_tail_mean':statistics.fmean(r['settle_tail_s'] for r in runs),
                'runs':runs
            }
    # hover windows from neutral dwell >=2s
    hover=[]
    i=0
    while i<len(usable):
        if max(abs(usable[i]['rc_elevator']),abs(usable[i]['rc_aileron']),abs(usable[i]['rc_throttle']),abs(usable[i]['rc_rudder']))>8:
            i+=1; continue
        j=i
        while j<len(usable) and max(abs(usable[j]['rc_elevator']),abs(usable[j]['rc_aileron']),abs(usable[j]['rc_throttle']),abs(usable[j]['rc_rudder']))<=8:
            j+=1
        dur=usable[j-1]['t']-usable[i]['t']
        if dur>=2.0:
            win=usable[i:j]
            hs=[mph_to_mps(math.hypot(r['vx_mph'],r['vz_mph'])) for r in win]
            vs=[mph_to_mps(r['vy_mph']) for r in win]
            alt=[r['alt_ft']*0.3048 for r in win]
            hover.append({'start_s':win[0]['t'],'end_s':win[-1]['t'],'duration_s':dur,'hspd_rms':(statistics.fmean(x*x for x in hs))**0.5,'vspd_rms':(statistics.fmean(x*x for x in vs))**0.5,'alt_std_m':statistics.pstdev(alt) if len(alt)>1 else 0.0})
        i=j
    if hover:
        out['hover_hold']={'count':len(hover),'hspd_rms_mean':statistics.fmean(h['hspd_rms'] for h in hover),'vspd_rms_mean':statistics.fmean(h['vspd_rms'] for h in hover),'alt_std_mean':statistics.fmean(h['alt_std_m'] for h in hover),'runs':hover}
    return out

def main():
    ap=argparse.ArgumentParser()
    ap.add_argument('csv')
    ap.add_argument('--json-out',default='Docs/airdata_mar30_analysis.json')
    args=ap.parse_args()
    rows=load(args.csv)
    usable,segs=segment(rows)
    metrics=compute_metrics(usable,segs)
    payload={'source_csv':args.csv,'total_rows':len(rows),'usable_rows':len(usable),'duration_s':usable[-1]['t']-usable[0]['t'],'segments':[s.__dict__|{'start_s':usable[s.start]['t'],'end_s':usable[s.end]['t']} for s in segs],'metrics':metrics}
    with open(args.json_out,'w',encoding='utf-8') as f: json.dump(payload,f,indent=2)
    print(json.dumps({'segments':len(segs),'maneuvers':sorted(set(s.maneuver for s in segs)),'json_out':args.json_out},indent=2))

if __name__=='__main__':
    main()
