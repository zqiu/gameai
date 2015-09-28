Ze Qin Qiu

1) For seperation in flocking, I modeled the force in seperation as a "target" So basically the closer 
the person to evade would be, the further away the target of my movement would be. This target would 
be along the same line formed by the agent and the person to evade but along the negative axis. This 
is pretty similar to the evade alogrith for dynamic wander. The distance behind the target would be set 
in a inverse correlation to the distance to the person to evade. Now that there's a target it was a simple
job of finding the necessary angular acceleration and linear acceleration to reach the imaginart "target" 
location.

2) The lecture mentions that of the three steering behaviours, the weights should go as such:
seperation > coalesse > alignment. So I picked values of 0.6, 0.3 and 0.1 respectively. It does seem like 
the seperation constant is a bit too strong.