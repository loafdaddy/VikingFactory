"""Original texture-free VikingFactory expansion. Python 3 + numpy + Pillow.
All geometry authored here. No extracted or copied game assets.
"""
from pathlib import Path
import numpy as np
from math import sin,cos,pi
import json,struct,math
ROOT=Path(__file__).resolve().parents[1]
MATERIALS=[('Oak',[.29,.17,.09,1],0,.92),('OakLight',[.46,.30,.16,1],0,.9),('Iron',[.15,.17,.18,1],.4,.75),('Bronze',[.43,.29,.13,1],.45,.72),('Hide',[.34,.25,.17,1],0,.98),('Stone',[.40,.42,.40,1],0,1)]
ALL=[]
def R(axis,a):
 c,s=cos(a),sin(a)
 if axis=='x':return np.array([[1,0,0],[0,c,-s],[0,s,c]])
 if axis=='y':return np.array([[c,0,s],[0,1,0],[-s,0,c]])
 return np.array([[c,-s,0],[s,c,0],[0,0,1]])
def basis(axis):
 return {'x':R('z',-pi/2),'y':np.eye(3),'z':R('x',pi/2),'-y':R('x',pi)}[axis]
class A:
 def __init__(self,id,title):
  self.id=id;self.title=title;self.groups={'Static':{'pos':[0,0,0],'extra':{},'parts':[]}};self.markers=[];self.cols=[];self.notes=[];ALL.append(self)
 def group(self,name,pos=(0,0,0),axis='y',**extras):
  self.groups[name]={'pos':list(pos),'extra':{'rotation_axis':axis,**extras},'parts':[]};return name
 def tri(self,v,f,mat,g='Static',uv=None):
  v=np.array(v,float);f=np.array(f,int);t=v[f];n=np.cross(t[:,1]-t[:,0],t[:,2]-t[:,0]);length=np.linalg.norm(n,axis=1)
  assert np.all(length>1e-10),self.id
  n/=length[:,None]
  if uv is None:
   # dimensioned projection, 0.5 metre repeat. Beam-specific UVs supplied below.
   uv=[]
   for face,no in zip(t,n):
    dims=[i for i in range(3) if i!=np.argmax(abs(no))];uv.append(face[:,dims]*2)
   uv=np.array(uv)
  else:uv=np.asarray(uv).reshape(-1,3,2)
  self.groups[g]['parts'].append({'v':t.reshape(-1,3),'n':np.repeat(n,3,axis=0),'uv':uv.reshape(-1,2),'mat':mat})
 def box(self,size,pos=(0,0,0),mat=0,g='Static',rotation=None,grain=1,collider=False):
  size=np.array(size);v=np.array([[-1,-1,-1],[1,-1,-1],[1,1,-1],[-1,1,-1],[-1,-1,1],[1,-1,1],[1,1,1],[-1,1,1]],float)*size/2
  f=np.array([[0,2,1],[0,3,2],[4,5,6],[4,6,7],[0,1,5],[0,5,4],[3,7,6],[3,6,2],[0,4,7],[0,7,3],[1,2,6],[1,6,5]])
  uv=[]
  for ids in f:
   pts=v[ids];n=np.cross(pts[1]-pts[0],pts[2]-pts[0]);normal=int(np.argmax(abs(n)));axes=[i for i in range(3) if i!=normal]
   # longitudinal U on beam sides; end grain uses the cross axes
   if grain in axes:axes=[grain,next(i for i in axes if i!=grain)]
   uv.append(pts[:,axes]*2)
  if rotation is not None:v=v@rotation.T
  self.tri(v+pos,f,mat,g,uv)
  if collider and g=='Static' and rotation is None:self.col('Box_'+str(len(self.cols)),size,pos)
 def beam(self,p,q,width=.12,depth=None,mat=0,g='Static'):
  p=np.array(p);q=np.array(q);d=q-p;l=np.linalg.norm(d);y=d/l;ref=np.array([1,0,0]) if abs(y[0])<.9 else np.array([0,0,1]);z=np.cross(ref,y);z/=np.linalg.norm(z);x=np.cross(y,z)
  self.box((width,l,depth or width),(p+q)/2,mat,g,np.column_stack([x,y,z]),grain=1)
 def cyl(self,r,h,pos=(0,0,0),mat=2,g='Static',axis='y',n=12,inner=0):
  verts=[];faces=[]
  if inner:
   for y in [-h/2,h/2]:
    for rr in [r,inner]:
     for i in range(n):verts.append([rr*cos(2*pi*i/n),y,rr*sin(2*pi*i/n)])
   for i in range(n):
    j=(i+1)%n
    for a,b,c,d in [(i,2*n+i,2*n+j,j),(n+i,n+j,3*n+j,3*n+i),(i,j,n+j,n+i),(2*n+i,3*n+i,3*n+j,2*n+j)]:faces.extend([[a,b,c],[a,c,d]])
  else:
   for y in [-h/2,h/2]:
    for i in range(n):verts.append([r*cos(2*pi*i/n),y,r*sin(2*pi*i/n)])
   verts.extend([[0,-h/2,0],[0,h/2,0]])
   for i in range(n):
    j=(i+1)%n;faces.extend([[i,n+i,n+j],[i,n+j,j],[2*n,i,j],[2*n+1,n+j,n+i]])
  v=np.array(verts);f=np.array(faces)
  if np.einsum('ij,ij->i',v[f[:,0]],np.cross(v[f[:,1]],v[f[:,2]])).sum()<0:f=f[:,::-1]
  # Cylindrical UVs: longitudinal grain U, circumference V; cap UVs planar.
  uv=[]
  for ids in f:
   pts=v[ids]
   if np.ptp(pts[:,1])<1e-8:uv.append(pts[:,[0,2]]*2)
   else:
    angle=np.arctan2(pts[:,2],pts[:,0]);
    if np.ptp(angle)>pi:angle=np.where(angle<0,angle+2*pi,angle)
    uv.append(np.column_stack([pts[:,1]*2,angle*r*2]))
  self.tri(v@basis(axis).T+pos,f,mat,g,uv)
 def cone(self,r,h,pos,mat=2,g='Static',axis='y',n=8):
  v=[[r*cos(i*2*pi/n),-h/2,r*sin(i*2*pi/n)] for i in range(n)]+[[0,h/2,0],[0,-h/2,0]];f=[]
  for i in range(n):j=(i+1)%n;f.extend([[i,n,j],[n+1,i,j]])
  vv=np.array(v);ff=np.array(f)
  if np.einsum('ij,ij->i',vv[ff[:,0]],np.cross(vv[ff[:,1]],vv[ff[:,2]])).sum()<0:ff=ff[:,::-1]
  self.tri(vv@basis(axis).T+pos,ff,mat,g)
 def tube(self,path,r=.025,mat=4,g='Static',closed=False,sides=6):
  path=np.array(path,float);segments=len(path) if closed else len(path)-1
  # Planar YZ/XZ paths; parallel frame by a stable reference axis.
  frames=[]
  for i,p in enumerate(path):
   if closed:t=path[(i+1)%len(path)]-path[(i-1)%len(path)]
   else:t=path[min(i+1,len(path)-1)]-path[max(i-1,0)]
   t/=np.linalg.norm(t);ref=np.array([1.,0,0]) if abs(t[0])<.9 else np.array([0.,0,1]);u=np.cross(t,ref);u/=np.linalg.norm(u);v=np.cross(t,u);frames.append((u,v))
  verts=np.array([[*(p+r*(cos(2*pi*j/sides)*u+sin(2*pi*j/sides)*v))] for p,(u,v) in zip(path,frames) for j in range(sides)])
  lengths=[0.]
  for i in range(segments):lengths.append(lengths[-1]+np.linalg.norm(path[(i+1)%len(path)]-path[i]))
  repeats=max(1,round(lengths[-1]/.5)) if closed else lengths[-1]/.5
  faces=[];uv=[]
  # Around the tube, one UV unit is 0.5 m, the same repeat as beams and the native timber clone.
  around=2*pi*r/0.5
  for i in range(segments):
   k=(i+1)%len(path);u0=lengths[i]/lengths[-1]*repeats;u1=lengths[i+1]/lengths[-1]*repeats
   for j in range(sides):
    j1=(j+1)%sides;a=i*sides+j;b=i*sides+j1;c=k*sides+j1;d=k*sides+j
    v0=j/sides*around;v1=j1/sides*around
    faces.extend([[a,b,c],[a,c,d]]);uv.extend([[[u0,v0],[u0,v1],[u1,v1]],[[u0,v0],[u1,v1],[u1,v0]]])
  # Check outward for the first side triangle; flip all if required.
  f=np.array(faces);tri=verts[f[0]];normal=np.cross(tri[1]-tri[0],tri[2]-tri[0]);radial=tri.mean(0)-(path[0]*2+path[1])/3
  if np.dot(normal,radial)<0:f=f[:,::-1];uv=np.array(uv)[:,::-1,:]
  self.tri(verts,f,mat,g,uv)
 def port(self,name,p,d,kind='kinetic'):self.markers.append({'name':name,'position':list(p),'forward':list(d),'kind':kind})
 def col(self,name,size,p):self.cols.append({'name':name,'parent':'Static','shape':'box','center':list(p),'size':list(size)})
 def foot(self,x,z,h=.7,w=.16):
  self.box((w,h,w),(x,h/2,z),0,collider=True);self.box((w+.08,.08,w+.08),(x,.08,z),2)
 def base(self,w,d,mat=0):self.box((w,.12,d),(0,.06,0),mat,grain=2,collider=True)
 def gear(self,r,pos,g,teeth=12,axis='x'):
  self.cyl(r*.78,.13,pos,0,g,axis,n=teeth*2);self.cyl(r*.25,.20,pos,3,g,axis)
  for i in range(teeth):
   t=i*2*pi/teeth
   if axis=='x':p=np.array(pos)+[0,r*.86*cos(t),r*.86*sin(t)];sz=(.15,r*.28,r*.20);rr=R('x',t)
   else:p=np.array(pos)+[r*.86*cos(t),0,r*.86*sin(t)];sz=(r*.28,.15,r*.20);rr=R('y',-t)
   self.box(sz,p,1,g,rr)
 def stone_ring(self,w,d):
  for x in np.linspace(-w/2+.15,w/2-.15,max(2,round(w/.4))):
   for z in [-d/2,d/2]:self.box((.36,.20,.27),(x,.10,z),5,collider=True)
  for z in np.linspace(-d/2+.35,d/2-.35,max(2,round(d/.4)-1)):
   for x in [-w/2,w/2]:self.box((.27,.20,.36),(x,.10,z),5,collider=True)
 def axle(self,z=0,y=.5,r=.1):
  self.cyl(r,.70,(0,y,z),2,axis='x');
  for x in [-.27,.27]:self.cyl(r+.055,.10,(x,y,z),3,axis='x',inner=r+.005)

def rope():
 a=A('vf_rope_4m','Rope transmission · 4 m')
 for z in [-1.80,1.80]:
  a.box((.55,.10,.40),(-.20,.05,z),0,grain=2,collider=True);a.foot(-.25,z,.72,.15)
  a.beam((-.43,.10,z),(-.25,.40,z),.10);a.beam((.04,.10,z),(-.25,.40,z),.10)
  a.cyl(.075,.62,(-.20,.5,z),3,axis='x');a.cyl(.12,.11,(-.25,.5,z),3,axis='x',inner=.08)
  # C-shaped forged hook supporting shaft immediately outside bearing.
  path=[[-.40,.5+.12*cos(t),z+.12*sin(t)] for t in np.linspace(-pi*.25,pi*1.15,13)];a.tube(path,.022,3)
  g=a.group('Rotor' if z<0 else 'Rotor_Output',[.04,.5,z],'x')
  a.cyl(.158,.10,(0,0,0),0,g,'x',16);a.cyl(.19,.025,(-.06,0,0),3,g,'x',16,inner=.14);a.cyl(.19,.025,(.06,0,0),3,g,'x',16,inner=.14)
 g=a.group('Belt_Surface',axis='x',scroll='u');path=[];r=.18
 for z in np.linspace(-1.8,1.8,23,endpoint=False):path.append([.04,.5+r-.10*(1-(z/1.8)**2),z])
 for t in np.linspace(0,pi,13,endpoint=False):path.append([.04,.5+r*cos(t),1.8+r*sin(t)])
 for z in np.linspace(1.8,-1.8,23,endpoint=False):path.append([.04,.5-r-.10*(1-(z/1.8)**2),z])
 for t in np.linspace(pi,2*pi,13,endpoint=False):path.append([.04,.5+r*cos(t),-1.8+r*sin(t)])
 a.tube(path,.025,4,g,True,6)
 a.port('Kinetic_In',[-.51,.5,-1.8],[-1,0,0]);a.port('Kinetic_Out',[-.51,.5,1.8],[-1,0,0]);a.port('Interact',[-.4,.7,-1.8],[-1,0,0],'service')
 a.notes+=['Nominal 4 m module; pulley centres 3.6 m apart. UV U follows the closed rope loop and advances by an integer at its seam.','Belt_Surface scrolls only; do not rotate its transform. Runtime scales/rebuilds sag for shorter spans.']

def reversing():
 a=A('vf_reversing_cog','Reversing cog pair');a.base(.8,1.5)
 for z in [-.36,.36]:
  for x in [-.25,.25]:a.foot(x,z,.48,.12)
  g=a.group('Rotor' if z<0 else 'Rotor_Output',[0,.5,z],'x');a.gear(.36,[0,0,0],g,12);a.cyl(.065,.74,(0,0,0),2,g,'x')
  for x in [-.25,.25]:a.cyl(.105,.08,(x,.5,z),3,axis='x',inner=.07)
 # Two chevron arrow inlays inset on fixed wood plates; physical surface detail.
 for z,sgn in [(-.36,1),(.36,-1)]:
  a.box((.04,.25,.34),(.40,.25,z),0)
  for dz in [-1,1]:a.beam((.422,.25+sgn*.075,z),(.422,.25-sgn*.055,z+dz*.095),.025,mat=1)
 a.port('Kinetic_In',[-.37,.5,-.36],[-1,0,0]);a.port('Kinetic_Out',[.37,.5,.36],[1,0,0]);a.notes+=['Two meshing external gears; Rotor_Output must run at the opposite angular velocity. Fixed timber chevrons indicate opposing rotation.']

def merger():
 a=A('vf_merger','Three-mouth merger')
 for x in [-.68,.68]:
  for z in [-.68,.68]:a.foot(x,z,.61)
 a.box((1.65,.12,1.65),(0,.65,0),0,grain=2,collider=True)
 for x,z in [(-.64,-.64),(.64,-.64),(-.64,.64),(.64,.64)]:
  a.box((.23,.24,.23),(x,.83,z),0,collider=True);a.box((.26,.045,.26),(x,.96,z),2)
 # segmented deck with grain direction aligned to incoming channels
 for x in np.linspace(-.64,.64,7):a.box((.16,.04,1.58),(x,.735,0),1,grain=2)
 g=a.group('Selector',[0,.80,0],'y');a.cyl(.11,.06,(0,0,0),3,g);a.box((.045,.035,.30),(0,.03,.06),3,g)
 for n,p,d in [('Item_In',[0,.80,-.85],[0,0,-1]),('Item_In_Left',[-.85,.80,0],[-1,0,0]),('Item_In_Right',[.85,.80,0],[1,0,0])]:a.port(n,p,d,'item_in')
 a.port('Item_Out',[0,.80,.85],[0,0,1],'item_out');a.port('Kinetic_In',[.85,.5,.65],[1,0,0]);a.notes+=['All four mouths remain open; selector is a low central bronze guide.']

def ironbelt():
 a=A('vf_iron_belt_2m','Iron chain-deck conveyor')
 for x in [-.46,.46]:
  for z in [-.74,.74]:a.foot(x,z,.62,.14)
  a.box((.10,.15,2),(x,.67,0),2,grain=2);a.box((.055,.11,2),(x,.86,0),2,grain=2)
 for z in [-.74,.74]:a.beam((-.46,.18,z),(.46,.60,z),.08,mat=2)
 g=a.group('Belt_Surface',[0,.77,0],'x',scroll='u')
 # closed flattened hide loop: longitudinal U on path, transverse V along X
 path=[]
 for t in np.linspace(0,2*pi,32,endpoint=False):
  z=.85*np.sign(sin(t)) if abs(sin(t))>.999 else .85*sin(t)
  # elliptical loop, hidden under slats on top
  path.append([0,.11*cos(t)-.08,.88*sin(t)])
 a.tube(path,.035,4,g,True,6)
 # fixed overlapping deck allows pooled item visuals; independent end sprockets
 for z in np.linspace(-.9,.9,13):
  a.box((.85,.065,.12),(0,.79,z),1,grain=0)
  for x in [-.38,.38]:a.box((.09,.035,.14),(x,.84,z),2)
 for z in [-.85,.85]:
  q=a.group('Roller_'+('Rear' if z<0 else 'Front'),[0,.70,z],'x');a.cyl(.13,1.10,(0,0,0),2,q,'x',12)
 a.col('Deck',[.86,.15,2],[0,.69,0]);a.port('Item_In',[0,.85,-1],[0,0,-1],'item_in');a.port('Item_Out',[0,.85,1],[0,0,1],'item_out');a.port('Kinetic_In',[.57,.5,-.75],[1,0,0]);a.notes+=['Iron ladder chassis, overlapping timber treads, forged wear caps and braced feet distinguish it from the timber tier. Decorative treads stay static; scroll only the loop or add runtime cyclic treads.']

def ironfeeder():
 a=A('vf_iron_feeder','Iron counterweighted feeder');a.base(.9,.9,5)
 for x in [-.25,.25]:a.box((.12,.72,.32),(x,.48,0),2,collider=True)
 a.cyl(.24,.14,(0,.87,0),3);g=a.group('Arm_Yaw',[0,.94,0],'y')
 for x in [-.15,.15]:
  a.beam((x,0,-.36),(x,.35,.70),.08,mat=2,g=g);a.beam((x,.35,.70),(x,.02,1.08),.065,mat=2,g=g)
 a.box((.45,.28,.25),(0,0,-.40),5,g);a.box((.4,.08,.18),(0,.02,1.08),2,g)
 for x in [-.18,.18]:a.box((.06,.24,.10),(x,-.12,1.08),2,g)
 a.cyl(.09,.43,(0,.35,.7),3,g,'x');a.port('Pickup',[0,.73,1.08],[0,0,1],'item_in');a.port('Dropoff',[0,.73,-1.08],[0,0,-1],'item_out');a.port('Kinetic_In',[.47,.5,0],[1,0,0]);a.notes+=['Twin forged arms, stone counterweight and iron clevis pedestal; not a recolour of the bronze feeder.']

def saw():
 a=A('vf_timber_saw','Reciprocating frame saw')
 for x in [-.70,.70]:
  for z in [-.72,.72]:a.foot(x,z,2.15,.18)
  a.box((.20,.20,1.7),(x,2.13,0),0,grain=2,collider=True)
 for z in [-.72,.72]:a.box((1.6,.20,.18),(0,2.13,z),0,grain=0,collider=True)
 # supports and rollers below unobstructed log passage
 for z in [-.75,.75]:
  a.box((1.4,.14,.17),(0,.49,z),0,grain=0,collider=True)
  g=a.group('Roller_'+str(z),[0,.63,z],'x');a.cyl(.085,1.18,(0,0,0),2,g,'x')
 g=a.group('Saw_Frame',[0,1.28,0],'y',motion='slide',travel=.28)
 for x in [-.56,.56]:a.box((.09,1.04,.10),(x,0,0),2,g)
 for y in [-.52,.52]:a.box((1.20,.09,.10),(0,y,0),0,g,grain=0)
 a.box((.045,.95,.09),(0,0,0),2,g)
 for y in np.linspace(-.42,.42,12):a.cone(.036,.055,(0,y,.065),2,g,'z',3)
 a.port('Item_In',[0,.79,-1],[0,0,-1],'item_in');a.port('Item_Out',[0,.79,1],[0,0,1],'item_out');a.port('Kinetic_In',[.85,.5,0],[1,0,0]);a.notes+=['Saw_Frame translates vertically ±0.14 m. Log axis is Z, supported at y=0.72. Only the cutting blade enters the open log path; no static collider spans the mouth.']

def bed(id,title,w,d,gate=False):
 a=A(id,title);a.stone_ring(w,d)
 for x in [-w/2,w/2]:
  for z in [-d/2,d/2]:a.foot(x,z,.72,.09);a.cone(.065,.15,(x,.77,z),0)
 if gate:
  for x in [-.55,.55]:a.foot(x,-d/2-.08,1,.12)
  g=a.group('Gate',[-.50,.55,-d/2-.08],'y',motion='swing')
  for y in [-.23,.10]:a.box((1,.09,.08),(.5,y,0),0,g,grain=0)
  for x in [.12,.5,.88]:a.box((.075,.57,.08),(x,-.06,0),1,g)
  # remove centre entrance stones & their colliders
  st=a.groups['Static'];st['parts']=[p for p in st['parts'] if not(p['mat']==5 and abs(p['v'][:,0].mean())<.65 and p['v'][:,2].mean() < -d/2+.2)]
  a.cols=[c for c in a.cols if not(abs(c['center'][0])<.65 and c['center'][2]<-d/2+.2)]
  a.port('Interact',[.48,.6,-d/2-.16],[0,0,-1],'service')
 a.port('Ground_Probe',[0,.03,0],[0,-1,0],'environment');a.notes+=['Open bed interior: actual terrain remains visible. No floor slab or central collider; vegetation is supplied by gameplay.']
 return a

def railtool(harvest=False):
 a=A('vf_harvester' if harvest else 'vf_planter','Rail harvester' if harvest else 'Rail planter')
 for x in [-1.25,1.25]:
  a.box((.16,.16,3.0),(x,.08,0),0,grain=2,collider=True);a.box((.045,.045,3.0),(x,.18,0),2,grain=2)
 g=a.group('Tool_Head',[0,0,0],'z',motion='slide',travel=2.2)
 for x in [-1.25,1.25]:
  a.box((.13,1.10,.13),(x,.7,0),0,g)
  for z in [-.24,.24]:a.cyl(.12,.12,(x,.27,z),2,g,'x')
 a.box((2.65,.16,.20),(0,1.25,0),0,g,grain=0)
 if harvest:
  a.box((1.6,.08,.12),(0,.26,.22),2,g,grain=0)
  for x in np.linspace(-.72,.72,10):a.cone(.075,.24,(x,.27,.39),2,g,'z',3)
  a.box((1.35,.07,.52),(0,.4,-.3),1,g,grain=0)
 else:
  for x in [-.65,0,.65]:
   a.box((.30,.30,.34),(x,.99,0),0,g);a.cyl(.055,.63,(x,.51,0),3,g);a.cone(.10,.16,(x,.18,0),2,g,axis='-y')
 a.port('Kinetic_In',[-1.4,.5,-1.5],[-1,0,0]);a.port('Item_Out' if harvest else 'Item_In',[0,.7,-1.5],[0,0,-1],'item_out' if harvest else 'item_in');a.port('Ground_Probe',[0,0,0],[0,-1,0],'environment');a.notes+=['Tool_Head is a complete travelling carriage along Z. Root marker is the parked service dock; moving operation points must be derived from the carriage.']

def engine(reinforced=False):
 a=A('vf_reinforced_steam_engine' if reinforced else 'vf_steam_engine','Ashlands reinforced engine' if reinforced else 'Stone boiler beam engine')
 w=2.5 if reinforced else 2.0;d=2.6 if reinforced else 2.15;a.base(w,d,5)
 # masonry firebox, open front fuel mouth at -Z
 for x in [-.48,.48]:a.box((.25,.48,1.30),(x,.36,-.20),5,collider=True)
 a.box((1.22,.18,1.3),(0,.70,-.20),5,collider=True);a.box((1.18,.48,.18),(0,.36,.42),5,collider=True)
 a.cyl(.53,.85,(0,1.18,-.20),2,n=16)
 for y in [.82,1.15,1.52]:a.cyl(.57,.065,(0,y,-.2),2,n=16,inner=.53)
 a.cone(.53,.25,(0,1.73,-.20),2,n=16)
 # smoke stack independent of moving mechanism
 a.cyl(.15,1.05,(0,2.12,-.20),2,n=10,inner=.105);a.cyl(.19,.08,(0,2.65,-.20),3,n=10,inner=.105)
 for z in [-.7,.8]:a.foot(.80,z,1.1,.17)
 a.box((.20,.15,1.7),(.80,1.15,.05),0,grain=2)
 g=a.group('Beam',[.80,1.27,.05],'x',motion='swing');a.box((.16,.16,1.58),(0,0,0),0,g,grain=2)
 for z in [-.65,.65]:a.cyl(.095,.26,(0,0,z),3,g,'x')
 a.cyl(.12,.54,(.8,.87,-.65),3)
 g=a.group('Rotor',[.88,.5,.73],'x');a.cyl(.43,.10,(0,0,0),2,g,'x',16,inner=.32)
 for t in [0,pi/3,2*pi/3]:a.box((.10,.78,.07),(0,0,0),0,g,R('x',t))
 a.cyl(.095,.40,(0,0,0),3,g,'x')
 a.foot(.80,.73,.44,.20)
 if reinforced:
  for z in [-.70,.30]:
   for x in [-.66,.66]:a.box((.18,1.30,.18),(x,.77,z),2,collider=True);a.box((.26,.09,.25),(x,1.45,z),3)
  a.box((1.8,.22,.26),(0,.2,-1.05),5,collider=True)
  a.notes+=['Stone and Iron remain the only stone/metal material names. Native grausten/flametal appearance requires asset-ID-aware runtime selection; no forbidden extra material names or textures are embedded.']
 a.port('Kinetic_Out',[1.10,.5,.73],[1,0,0]);a.port('Water_Probe',[-.67,.40,.25],[-1,0,0],'water_connection');a.port('Smoke_Out',[0,2.72,-.20],[0,1,0],'environment');a.port('Item_In',[0,.38,-.95],[0,0,-1],'fuel');a.port('Interact',[.65,.9,-.98],[0,0,-1],'service');a.notes+=['Smoke outlet is above and separated from beam/rotor. Firebox opening is clear. Beam rocks; wheel rotates. No explosion, gauge or pressure dial geometry.']

def intake():
 a=A('vf_water_intake','Short water intake');a.base(.65,1.3,5)
 a.cyl(.12,1.30,(0,.33,0),2,axis='z',n=10,inner=.08)
 for z in [-.42,.42]:a.cyl(.155,.075,(0,.33,z),3,axis='z',n=10,inner=.12)
 for x in [-.10,0,.10]:a.box((.025,.21,.03),(x,.33,-.67),2)
 a.port('Water_Probe',[0,.33,-.73],[0,0,-1],'environment');a.port('Interact',[0,.33,.73],[0,0,1],'water_connection');a.notes+=['Short open pipe with bronze collars; Interact marks boiler-side coupling.']

def sail():
 a=A('vf_sail_wheel','Linen sail wheel');a.base(1.25,1.45,5)
 for x in [-.40,.40]:a.foot(x,0,2.4,.17);a.beam((x,.1,-.6),(x,1.5,0),.13)
 a.box((1.15,.18,.30),(0,2.40,0),0,grain=0)
 g=a.group('Rotor',[0,2.40,-.28],'z');a.cyl(.21,.5,(0,0,0),2,g,'z')
 for i in range(4):
  t=i*pi/2
  def tr(p):return R('z',t)@p
  a.beam(tr([0,.1,0]),tr([0,1.9,0]),.10,mat=0,g=g)
  a.box((.49,1.1,.018),tr([.23,1.2,0]),4,g,R('z',t),grain=1)
  for yy in [.66,1.2,1.75]:a.box((.53,.045,.05),tr([.23,yy,-.03]),1,g,R('z',t),grain=0)
 a.beam((0,2.40,.05),(0,2.40,1.50),.09);a.box((.03,.53,.68),(0,2.43,1.40),1)
 a.port('Kinetic_Out',[0,2.40,.25],[0,0,1]);a.port('Wind_Probe',[0,3.5,-.45],[0,0,-1],'environment');a.notes+=['Axle height exception: 2.4 m, required by sail clearance. Hide is the mandated sail slot; runtime should select a linen-compatible native surface by this asset ID.']

def flywheel():
 a=A('vf_flywheel','Heavy timber flywheel');a.base(.85,1.0,5)
 for x in [-.30,.30]:a.foot(x,0,.48,.17);a.cyl(.12,.13,(x,.5,0),3,axis='x',inner=.07)
 g=a.group('Rotor',[0,.5,0],'x');a.cyl(.45,.21,(0,0,0),0,g,'x',20,inner=.28)
 for x in [-.13,.13]:a.cyl(.47,.04,(x,0,0),2,g,'x',20,inner=.39)
 for t in [0,pi/3,2*pi/3]:a.box((.12,.82,.08),(0,0,0),0,g,R('x',t))
 a.cyl(.085,.8,(0,0,0),3,g,'x');a.port('Kinetic_Left',[-.4,.5,0],[-1,0,0]);a.port('Kinetic_Right',[.4,.5,0],[1,0,0])

def governor():
 a=A('vf_governor','Centrifugal governor');a.base(.7,.7);a.cyl(.10,.13,(0,.19,0),3)
 g=a.group('Rotor',[0,.22,0],'y');a.cyl(.055,.75,(0,.37,0),2,g)
 for x in [-1,1]:
  a.beam((0,.67,0),(x*.27,.35,0),.05,mat=0,g=g);a.beam((0,.10,0),(x*.27,.35,0),.05,mat=0,g=g)
  # two low-poly iron balls: octahedra rounded by two pyramids
  verts=[[x*.27,.48,0],[x*.27,.22,0]]
  for latitude in [-pi/4,0,pi/4]:
   for j in range(8):verts.append([x*.27+.13*cos(latitude)*cos(j*pi/4),.35+.13*sin(latitude),.13*cos(latitude)*sin(j*pi/4)])
  faces=[]
  for j in range(8):
   k=(j+1)%8;faces.extend([[1,2+k,2+j],[0,18+j,18+k]])
   for ring in range(2):
    aa=2+8*ring+j;bb=2+8*ring+k;cc=bb+8;dd=aa+8;faces.extend([[aa,bb,cc],[aa,cc,dd]])
  vv=np.array(verts);ff=np.array(faces)
  if np.einsum('ij,ij->i',vv[ff[:,0]],np.cross(vv[ff[:,1]],vv[ff[:,2]])).sum()<0:ff=ff[:,::-1]
  a.tri(vv,ff,2,g)
 a.cyl(.12,.07,(0,.35,0),3,g)
 for x in [-.28,.28]:a.foot(x,0,.82,.08)
 a.box((.68,.10,.12),(0,.86,0),0,grain=0);a.port('Kinetic_In',[0,.15,-.36],[0,0,-1]);a.port('Interact',[.37,.5,0],[1,0,0],'service')

def drill(deep=False):
 a=A('vf_deep_extractor' if deep else 'vf_finite_mining_head','Deep extraction derrick' if deep else 'Finite mining head')
 w=1.8 if deep else 1.3;h=3.3 if deep else 2.1;zspan=1.0 if deep else .75
 for x in [-w/2,w/2]:
  for z in [-zspan,zspan]:
   a.foot(x,z,h,.19 if deep else .15)
   for y in [.2,h*.55,h-.15]:a.box((.23,.12,.23),(x,y,z),2)
 for z in [-zspan,zspan]:
  a.box((w+.3,.20,.20),(0,h,z),0,grain=0)
  a.beam((-w/2,.30,z),(w/2,h-.20,z),.10,mat=2 if deep else 0)
 a.box((.30,.20,zspan*2+.2),(0,h,0),0,grain=2)
 g=a.group('Drill',[0,h*.50,0],'y');a.cyl(.13 if deep else .09,h-.35,(0,0,0),2,g);a.cone(.22 if deep else .15,.28,(0,-h*.5+.18,0),2,g,axis='-y')
 for i in range(18 if deep else 12):
  yy=-h*.38+i*(h*.7/(18 if deep else 12));a.box((.55 if deep else .36,.05,.12),(0,yy,0),2,g,R('y',i*.65))
 a.cyl(.26,.20,(0,h-.20,0),3);a.box((.55,.09,.90),(0,.34,zspan+.10),1,grain=2)
 a.port('Kinetic_In',[w/2+.15,.5,0],[1,0,0]);a.port('Item_Out',[0,.40,zspan+.55],[0,0,1],'item_out');a.port('Ground_Probe',[0,0,0],[0,-1,0],'environment');a.notes+=['Drill is vertical; perimeter-only colliders keep bore and output clear.']
 if deep:a.notes+=['Iron is the named metal slot; black-metal variant must be selected by runtime using vf_deep_extractor.']

def assembler():
 a=A('vf_advanced_assembler','Roofed multi-tool assembler')
 for x in [-1.05,1.05]:
  for z in [-.75,.75]:a.foot(x,z,2.30,.16)
 a.box((2.25,.16,1.65),(0,.88,0),0,grain=0,collider=True)
 for x in np.linspace(-1,1,9):a.box((.22,.05,1.6),(x,.99,0),1,grain=2)
 for x,sgn in [(-.62,1),(.62,-1)]:a.box((1.42,.09,1.95),(x,2.48,0),1,rotation=R('z',sgn*.35),grain=2)
 a.box((2.22,.14,.14),(0,2.1,0),0,grain=0)
 for i,x in enumerate([-.65,0,.65]):
  g=a.group('Tool_Head_'+str(i),[x,1.60,0],'y',motion='slide',travel=.22);a.cyl(.055,.75,(0,.15,0),2,g)
  if i==0:a.box((.28,.15,.28),(0,-.20,0),2,g)
  elif i==1:a.cone(.12,.23,(0,-.21,0),2,g)
  else:
   a.box((.34,.08,.13),(0,-.15,0),3,g)
   for xx in [-.14,.14]:a.box((.04,.15,.07),(xx,-.25,0),2,g)
 a.port('Item_In',[0,1.05,-.88],[0,0,-1],'item_in');a.port('Item_Out',[0,1.05,.88],[0,0,1],'item_out');a.port('Kinetic_In',[-1.20,.5,0],[-1,0,0]);a.notes+=['Roofed table with three separate tool heads. Roof remains static.']

def gantry():
 a=A('vf_farm_gantry','Twin-head field gantry')
 for x in [-2.0,2.0]:
  a.box((.22,.20,4.0),(x,.10,0),0,grain=2,collider=True);a.box((.05,.05,4),(x,.225,0),2,grain=2)
 g=a.group('Carriage',[0,0,0],'z',motion='slide',travel=3.0)
 for x in [-2,2]:
  a.box((.19,1.85,.19),(x,1.17,0),0,g)
  for z in [-.34,.34]:a.cyl(.18,.15,(x,.4,z),2,g,'x')
 a.box((4.22,.24,.28),(0,2.07,0),0,g,grain=0)
 # Tool heads are root-level independent groups; runtime must follow carriage Z.
 for i,x in enumerate([-.9,.9]):
  q=a.group('Tool_Head_'+str(i),[x,1.08,0],'y',motion='slide',follow_group='Carriage')
  a.cyl(.07,1.80,(0,0,0),2,q);a.box((.65,.12,.24),(0,-.70,0),2,q)
  for xx in [-.25,0,.25]:a.cone(.08,.24,(xx,-.85,0),2,q,axis='-y')
 a.port('Kinetic_In',[-2.2,.5,-2],[-1,0,0]);a.port('Item_In',[0,.60,-2],[0,0,-1],'item_in');a.port('Item_Out',[0,.60,2],[0,0,1],'item_out');a.port('Ground_Probe',[0,0,0],[0,-1,0],'environment');a.notes+=['Carriage travels along Z. Root-level tool heads follow Carriage translation and retain independent strokes. No collision slab through growing area.']

def eitr():
 a=A('vf_eitr_motor','Contained eitr motor');a.base(1.3,1.3,5)
 for x in [-.47,.47]:
  for z in [-.47,.47]:a.box((.23,.72,.23),(x,.48,z),5,collider=True);a.beam((x,.88,z),(x*.62,1.15,z*.62),.08,mat=2)
 a.cyl(.44,.10,(0,1.14,0),2,n=8,inner=.29)
 # contained faceted core, no emissive channel or embedded texture
 a.cone(.20,.46,(0,.77,0),3,n=6);a.cyl(.18,.24,(0,.50,0),3,n=6)
 g=a.group('Rotor',[0,.5,0],'x');a.cyl(.32,.10,(0,0,0),2,g,'x',12,inner=.26);a.cyl(.07,1.5,(0,0,0),2,g,'x')
 for x in [-.52,.52]:a.box((.18,.38,.22),(x,.31,0),5,collider=True);a.cyl(.11,.14,(x,.5,0),3,axis='x',inner=.075)
 a.port('Kinetic_Left',[-.75,.5,0],[-1,0,0]);a.port('Kinetic_Right',[.75,.5,0],[1,0,0]);a.port('Item_In',[0,.45,-.7],[0,0,-1],'fuel');a.notes+=['Contained faceted core suggests energy geometrically; no emission, neon or UI runes. Black marble/black metal must be selected by asset-ID-aware runtime material binding.']

def tender():
 a=A('vf_cooking_tender','Hearth-side tender');a.base(.65,.65,5);a.foot(0,0,.70,.16)
 g=a.group('Arm_Yaw',[0,.74,0],'y',motion='swing');a.cyl(.13,.12,(0,0,0),3,g);a.beam((0,.02,0),(0,.13,.95),.075,mat=2,g=g);a.box((.26,.04,.10),(0,.13,.95),3,g)
 for x in [-.10,.10]:a.box((.035,.10,.28),(x,.10,1.04),2,g,grain=2)
 a.port('Pickup',[0,.80,1.12],[0,0,1],'item_in');a.port('Dropoff',[0,.8,-.65],[0,0,-1],'item_out');a.port('Kinetic_In',[-.36,.5,0],[-1,0,0]);a.notes+=['Standalone reaching arm only; no vanilla hearth or cooking-rack mesh included.']

def oven():
 a=A('vf_oven_extension','Open hood extension')
 for x in [-.85,.85]:a.box((.24,1.45,.75),(x,.725,0),5,collider=True)
 a.box((1.95,.20,.90),(0,1.55,0),5,collider=True)
 for x in [-.40,.40]:a.box((1.05,.07,.95),(x,1.82,0),2,rotation=R('z',.43 if x<0 else -.43),grain=2)
 a.cyl(.18,.65,(0,2.16,0),2,n=10,inner=.13);a.port('Smoke_Out',[0,2.52,0],[0,1,0],'environment');a.port('Interact',[0,1,-.48],[0,0,-1],'service');a.notes+=['Open under the canopy and on both Z faces; no existing oven mesh or inner solid collider. Footprint must be checked against the target station in game.']

def livestock(culling=False):
 a=A('vf_livestock_culling_gate' if culling else 'vf_livestock_feed_gate','Livestock sorting gate' if culling else 'Livestock feeding gate')
 for x in [-.8,.8]:a.foot(x,0,1.20,.15)
 a.box((1.8,.14,.18),(0,1.2,0),0,grain=0)
 g=a.group('Gate',[-.72,.58,0],'y',motion='swing')
 for y in [-.30,0,.30]:a.box((1.44,.10,.09),(.72,y,0),0,g,grain=0)
 a.beam((.03,-.30,0),(1.40,.30,0),.065,mat=1,g=g)
 for y in [-.28,.28]:a.cyl(.055,.12,(0,y,0),3,g)
 if culling:
  # fork fence makes this a humane-looking physical sorting junction
  for sign in [-1,1]:
   for z in [.55,1.25]:a.foot(sign*(.80+z*.5),z,.95,.10)
   for y in [.4,.75]:a.beam((sign*.8,y,0),(sign*1.425,y,1.25),.085)
  a.port('Dropoff',[0,.15,1.30],[0,0,1],'sorting_exit');a.notes+=['Sorting gate with diverging lanes, no blade or gore. Gameplay culling is not represented visually.']
 else:
  # open trough outside passage
  a.box((1.4,.08,.50),(0,.12,-.60),0,grain=0,collider=True)
  for z in [-.88,-.32]:a.box((1.50,.30,.07),(0,.30,z),1,grain=0,collider=True)
  for x in [-.74,.74]:a.box((.07,.30,.6),(x,.30,-.60),1,collider=True)
  a.port('Item_In',[0,.48,-.95],[0,0,-1],'item_in')
 a.port('Kinetic_In',[-.95,.5,0],[-1,0,0]);a.port('Interact',[.65,.80,-.16],[0,0,-1],'service');a.notes+=['Static collider data covers posts/fence/trough only. Add a moving gate collider attached to Gate if gameplay requires collision during its swing.']

def winch():
 a=A('vf_fishing_winch','Waterside fishing winch');a.base(1.05,1.3)
 for x in [-.36,.36]:a.foot(x,0,.50,.15);a.cyl(.12,.12,(x,.5,0),3,axis='x',inner=.075)
 g=a.group('Rotor',[0,.5,0],'x');a.cyl(.19,.58,(0,0,0),0,g,'x')
 for x in [-.34,.34]:a.cyl(.31,.06,(x,0,0),0,g,'x',14)
 # several loops around the drum, part of rotor
 for x in np.linspace(-.24,.24,7):a.cyl(.217,.045,(x,0,0),4,g,'x',16,inner=.19)
 a.beam((0,.20,.5),(0,1.10,.88),.12);a.cyl(.10,.16,(0,1.1,.88),3,axis='x',inner=.065)
 line=a.group('Line_Surface',axis='x',scroll='u');a.tube([[0,.7,0],[0,1.08,.88],[0,.75,1.27],[0,.35,1.60]],.016,4,line)
 hook=[[0,.28+.08*cos(t),1.60+.08*sin(t)] for t in np.linspace(0,pi*1.5,15)];a.tube(hook,.022,3)
 a.port('Kinetic_Left',[-.45,.5,0],[-1,0,0]);a.port('Kinetic_Right',[.45,.5,0],[1,0,0]);a.port('Water_Probe',[0,.12,1.65],[0,-1,0],'environment');a.port('Item_Out',[0,.40,-.70],[0,0,-1],'item_out');a.notes+=['Rotor is drum/spool; Line_Surface uses longitudinal UVs, no physical rope simulation.']

def build():
 rope();reversing();merger();ironbelt();ironfeeder();saw();bed('vf_coppice_bed','Coppice nursery bed',4,4,True);railtool();railtool(True);engine();intake();sail();flywheel();governor();drill();drill(True);assembler();gantry();eitr();tender();oven();livestock();livestock(True);winch();engine(True);bed('vf_forage_bed','Wild forage bed',2.8,2.8)

class GLB:
 def __init__(self):self.nodes=[];self.meshes=[];self.views=[];self.access=[];self.blob=bytearray();self.roots=[]
 def acc(self,arr,typ):
  while len(self.blob)%4:self.blob.append(0)
  arr=np.array(arr,dtype='<f4');start=len(self.blob);self.blob.extend(arr.tobytes());self.views.append({'buffer':0,'byteOffset':start,'byteLength':arr.nbytes,'target':34962});a={'bufferView':len(self.views)-1,'componentType':5126,'count':len(arr),'type':typ}
  if typ=='VEC3':a.update(min=arr.min(0).tolist(),max=arr.max(0).tolist())
  self.access.append(a);return len(self.access)-1
 def node(self,n,parent=None):
  i=len(self.nodes);self.nodes.append(n)
  if parent is not None:self.nodes[parent].setdefault('children',[]).append(i)
  return i
 def add(self,a,offset=(0,0,0)):
  root=self.node({'name':a.id,'translation':list(offset),'extras':{'material_profile':'valheim','asset_id':a.id,'units':'metres'}});self.roots.append(root)
  for name,g in a.groups.items():
   gn=self.node({'name':name,'translation':g['pos'],'extras':g['extra']},root)
   for mat in range(6):
    parts=[p for p in g['parts'] if p['mat']==mat]
    if not parts:continue
    v=np.concatenate([p['v'] for p in parts]);n=np.concatenate([p['n'] for p in parts]);uv=np.concatenate([p['uv'] for p in parts]);attrs={'POSITION':self.acc(v,'VEC3'),'NORMAL':self.acc(n,'VEC3'),'TEXCOORD_0':self.acc(uv,'VEC2')}
    self.meshes.append({'name':a.id+'_'+name+'_'+MATERIALS[mat][0],'primitives':[{'attributes':attrs,'material':mat,'mode':4}]});self.node({'name':name+'_'+MATERIALS[mat][0],'mesh':len(self.meshes)-1},gn)
   if name=='Static':
    for c in a.cols:self.node({'name':'Collider_'+c['name'],'translation':c['center'],'extras':{'collider':'box','size':c['size'],'render':False}},gn)
  for m in a.markers:self.node({'name':m['name'],'translation':m['position'],'extras':{'kind':m['kind']}},root)
 def save(self,p):
  mats=[{'name':n,'pbrMetallicRoughness':{'baseColorFactor':c,'metallicFactor':m,'roughnessFactor':r}} for n,c,m,r in MATERIALS]
  d={'asset':{'version':'2.0','generator':'VikingFactory original procedural expansion'},'scene':0,'scenes':[{'nodes':self.roots}],'nodes':self.nodes,'meshes':self.meshes,'materials':mats,'buffers':[{'byteLength':len(self.blob)}],'bufferViews':self.views,'accessors':self.access}
  js=json.dumps(d,separators=(',',':')).encode();js+=b' '*((-len(js))%4);self.blob+=b'\0'*((-len(self.blob))%4)
  raw=struct.pack('<4sII',b'glTF',2,28+len(js)+len(self.blob))+struct.pack('<I4s',len(js),b'JSON')+js+struct.pack('<I4s',len(self.blob),b'BIN\0')+self.blob;p.write_bytes(raw)

def metadata(a):
 pts=np.concatenate([p['v']+g['pos'] for g in a.groups.values() for p in g['parts']]);bounds=[pts.min(0).tolist(),pts.max(0).tolist()];count=sum(len(p['v'])//3 for g in a.groups.values() for p in g['parts'])
 return {'id':a.id,'title':a.title,'material_profile':'valheim','coordinates':'right-handed, Y-up, metres; scale 1','front':'view toward origin from negative Z','bounds':bounds,'triangles':count,'parts':[{'name':n,'pivot':g['pos'],**g['extra']} for n,g in a.groups.items()],'markers':a.markers,'box_colliders':a.cols,'notes':a.notes,'limitations':'Native material cloning, procedural motion and working game components are runtime work; no textures, shaders, physics simulation or game scripts are embedded.'}

def export():
 big=GLB();records=[]
 for i,a in enumerate(ALL):
  d=ROOT/'models'/a.id;d.mkdir(parents=True,exist_ok=True);g=GLB();g.add(a);g.save(d/(a.id+'.glb'));meta=metadata(a);records.append(meta);(d/(a.id+'.json')).write_text(json.dumps(meta,indent=2))
  offset=[(i%5)*7,0,(i//5)*7];big.add(a,offset);meta['combined_scene_offset']=offset
 big.save(ROOT/'VikingFactory-Expansion-All.glb');(ROOT/'catalog.json').write_text(json.dumps({'version':'1.0','assets':records},indent=2));print('Built',len(ALL),'machines;',sum(m['triangles'] for m in records),'triangles; max',max(m['triangles'] for m in records))

# Software daylight renderer. Rasterises the supplied mesh triangles, not generated concept art.
def render_asset(a,az,el,W=900,H=620):
 from PIL import Image,ImageDraw
 az=math.radians(az);el=math.radians(el);eye=np.array([cos(el)*sin(az),sin(el),-cos(el)*cos(az)]);right=np.cross([0,1,0],eye);right/=np.linalg.norm(right);up=np.cross(eye,right)
 polys=[];allpts=[];light=np.array([-.5,.85,-.45]);light/=np.linalg.norm(light)
 for g in a.groups.values():
  for p in g['parts']:
   tris=(p['v']+g['pos']).reshape(-1,3,3);norm=p['n'][::3];base=np.array(MATERIALS[p['mat']][1][:3])
   for t,n in zip(tris,norm):
    allpts.extend(t);shade=.57+.43*max(0,float(n@light));color=np.clip((base*shade)**(1/1.65)*255,0,255).astype(int);polys.append((t,n,tuple(color)))
 allpts=np.array(allpts);coords=np.column_stack([allpts@right,allpts@up]);lo=coords.min(0);hi=coords.max(0);span=hi-lo;scale=min((W-110)/max(span[0],.1),(H-130)/max(span[1],.1));center=(lo+hi)/2
 def proj(v):
  q=np.column_stack([v@right,v@up]);return np.column_stack([(q[:,0]-center[0])*scale+W/2,-(q[:,1]-center[1])*scale+H/2+15])
 img=Image.new('RGB',(W,H),(228,229,223));draw=ImageDraw.Draw(img)
 # Ground shadow projection along daylight direction; neutral ground stays flat.
 for t,n,col in polys:
  s=t.copy();s[:,0]-=s[:,1]*light[0]/light[1];s[:,2]-=s[:,1]*light[2]/light[1];s[:,1]=-.006;draw.polygon([tuple(x) for x in proj(s)],fill=(193,195,186))
 # Correct per-pixel depth test prevents painter-order intersection artifacts.
 depth=np.full((H,W),-np.inf);pixels=np.array(img)
 for t,n,color in polys:
  if np.dot(n,eye)<-1e-6:continue
  p=proj(t);xmin=max(0,int(np.floor(p[:,0].min())));xmax=min(W-1,int(np.ceil(p[:,0].max())));ymin=max(0,int(np.floor(p[:,1].min())));ymax=min(H-1,int(np.ceil(p[:,1].max())))
  if xmin>xmax or ymin>ymax:continue
  den=(p[1,1]-p[2,1])*(p[0,0]-p[2,0])+(p[2,0]-p[1,0])*(p[0,1]-p[2,1])
  if abs(den)<1e-8:continue
  xx,yy=np.meshgrid(np.arange(xmin,xmax+1)+.5,np.arange(ymin,ymax+1)+.5)
  w0=((p[1,1]-p[2,1])*(xx-p[2,0])+(p[2,0]-p[1,0])*(yy-p[2,1]))/den
  w1=((p[2,1]-p[0,1])*(xx-p[2,0])+(p[0,0]-p[2,0])*(yy-p[2,1]))/den;w2=1-w0-w1
  z=w0*(t[0]@eye)+w1*(t[1]@eye)+w2*(t[2]@eye);dep=depth[ymin:ymax+1,xmin:xmax+1];mask=(w0>=-1e-7)&(w1>=-1e-7)&(w2>=-1e-7)&(z>dep);dep[mask]=z[mask];pixels[ymin:ymax+1,xmin:xmax+1][mask]=color
 return Image.fromarray(pixels)

def previews():
 from PIL import Image,ImageDraw,ImageFont
 fontpath='/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf'
 def font(n):return ImageFont.truetype(fontpath,n)
 thumbs=[]
 for a in ALL:
  sheet=Image.new('RGB',(1800,490),(228,229,223));d=ImageDraw.Draw(sheet);d.text((24,15),a.id+'  |  '+str(metadata(a)['triangles'])+' triangles',font=font(24),fill=(45,49,45))
  for j,(label,az,el) in enumerate([('FRONT',0,0),('SIDE',90,0),('THREE-QUARTER',125,24)]):
   im=render_asset(a,az,el,600,410);sheet.paste(im,(600*j,65));d.text((600*j+24,53),label,font=font(15),fill=(70,77,68))
   if j==2:thumbs.append((a,im))
  sheet.save(ROOT/'views'/(a.id+'-views.png'))
 cols=4;rows=math.ceil(len(thumbs)/cols);contact=Image.new('RGB',(cols*480,rows*365+100),(228,229,223));d=ImageDraw.Draw(contact);d.text((25,22),'VIKINGFACTORY / MISSING MACHINES',font=font(34),fill=(44,48,41));d.text((25,66),'Original geometry · texture-free · native-material slots · 26 machines',font=font(19),fill=(90,95,83))
 for i,(a,im) in enumerate(thumbs):
  x=(i%cols)*480;y=(i//cols)*365+100;contact.paste(im.resize((480,328)),(x,y+25));d.text((x+16,y+7),a.id,font=font(17),fill=(49,54,45))
 contact.save(ROOT/'Expansion-Contact-Sheet.png');print('Rendered',len(ALL),'three-view sheets')
if __name__=='__main__':
 build();export();previews()
