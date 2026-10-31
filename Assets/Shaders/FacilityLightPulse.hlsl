#ifndef DARKLORD_FACILITY_LIGHT_PULSE
#define DARKLORD_FACILITY_LIGHT_PULSE
half FacilityLightPulse(float overrideTime)
{
    float time = overrideTime >= 0 ? overrideTime : _Time.y;
    return .25h + .75h * (.5h + .5h * sin(time * (6.28318530718 / 2.8)));
}
#endif
