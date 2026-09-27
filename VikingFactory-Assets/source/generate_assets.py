"""Original procedural VikingFactory prototype geometry. Requires Python + numpy.
Run from any directory. Optional preview renderer requires matplotlib.
Coordinate contract: metres, right-handed, Y up, root origin on floor.
"""
from pathlib import Path
import json, math, struct
import numpy as np
ROOT=Path(__file__).resolve().parents[1]
MATS=[('Oak',[.32,.17,.075,1],0,.9),('OakLight',[.49,.30,.14,1],0,.85),('Iron',[.12,.145,.15,1],.65,.62),('Bronze',[.49,.30,.105,1],.55,.57),('Hide',[.22,.115,.065,1],0,.98),('Stone',[.32,.35,.34,1],0,1)]
ASSETS=[]
def rot(axis,a):
 c,s=math.cos(a),math.sin(a)
 if axis=='x':return np.array([[1,0,0],[0,c,-s],[0,s,c]])
 if axis=='y':return np.array([[c,0,s],[0,1,0],[-s,0,c]])
 return np.array([[c,-s,0],[s,c,0],[0,0,1]])
def clean(v,f):
 v=np.asarray(v,float);f=np.asarray(f,int)
 vol=np.einsum('ij,ij->i',v[f[:,0]],np.cross(v[f[:,1]],v[f[:,2]])).sum()/6
 if vol<0:f=f[:,::-1]
 return v,f
class Asset:
 def __init__(self,name,label):
  self.name=name;self.label=label;self.groups={'Static':{'pivot':[0,0,0],'axis':None,'parts':[]}};self.markers=[];self.colliders=[];ASSETS.append(self)
 def group(self,name,pivot,axis):self.groups[name]={'pivot':pivot,'axis':axis,'parts':[]};return name
 def mesh(self,v,f,mat=0,group='Static'):
  v,f=clean(v,f);self.groups[group]['parts'].append((v,f,mat))
 def box(self,size,at=(0,0,0),mat=0,group='Static',rotation=None):
  v=np.array([[-1,-1,-1],[1,-1,-1],[1,1,-1],[-1,1,-1],[-1,-1,1],[1,-1,1],[1,1,1],[-1,1,1]],float)*np.array(size)/2
  if rotation is not None:v=v@rotation.T
  f=[[0,2,1],[0,3,2],[4,5,6],[4,6,7],[0,1,5],[0,5,4],[3,7,6],[3,6,2],[0,4,7],[0,7,3],[1,2,6],[1,6,5]]
  self.mesh(v+at,f,mat,group)
 def cyl(self,r,length,at=(0,0,0),mat=2,group='Static',axis='y',n=12,inner=0):
  # build annular/capped extrusion along Y, then rotate to the requested axis
  vs=[];fs=[]
  if inner:
   for y in [-length/2,length/2]:
    for radius in [r,inner]:
     for i in range(n):vs.append([radius*math.cos(2*math.pi*i/n),y,radius*math.sin(2*math.pi*i/n)])
   for i in range(n):
    j=(i+1)%n
    for a,b,c,d in [(i,j,2*n+j,2*n+i),(n+j,n+i,3*n+i,3*n+j),(j,i,n+i,n+j),(2*n+i,2*n+j,3*n+j,3*n+i)]:fs.extend([[a,b,c],[a,c,d]])
  else:
   for y in [-length/2,length/2]:
    for i in range(n):vs.append([r*math.cos(2*math.pi*i/n),y,r*math.sin(2*math.pi*i/n)])
   vs += [[0,-length/2,0],[0,length/2,0]]
   for i in range(n):
    j=(i+1)%n;fs.extend([[i,j,n+j],[i,n+j,n+i],[2*n,j,i],[2*n+1,n+i,n+j]])
  v=np.array(vs)
  if axis=='x':v=v@rot('z',-math.pi/2).T
  if axis=='z':v=v@rot('x',math.pi/2).T
  self.mesh(v+at,fs,mat,group)
 def beam(self,a,b,width=.12,depth=None,mat=0,group='Static'):
  a=np.array(a);b=np.array(b);direction=b-a;l=np.linalg.norm(direction);y=direction/l
  ref=np.array([1,0,0]) if abs(y[0])<.9 else np.array([0,0,1]);z=np.cross(ref,y);z/=np.linalg.norm(z);x=np.cross(y,z)
  self.box((width,l,depth or width),(a+b)/2,mat,group,np.column_stack([x,y,z]))
 def port(self,name,at,direction,kind):self.markers.append({'name':name,'position':list(at),'forward':list(direction),'kind':kind})
 def collider(self,name,size,at):self.colliders.append({'name':name,'type':'box','size':list(size),'center':list(at),'parent':'Static'})
 def feet(self,w,d,h):
  for x in [-w/2,w/2]:
   for z in [-d/2,d/2]:
    self.box((.18,h,.18),(x,h/2,z));self.box((.25,.12,.25),(x,.08,z),2)
 def gear(self,r,thick,at,group,axis='x',n=12):
  self.cyl(r*.78,thick,at,0,group,axis,n*2)
  self.cyl(r*.30,thick*1.45,at,3,group,axis,12)
  for i in range(n):
   t=i*2*math.pi/n
   if axis=='x':p=np.array(at)+[0,r*.84*math.cos(t),r*.84*math.sin(t)];sz=(thick,r*.30,r*.20);R=rot('x',t)
   else:p=np.array(at)+[r*.84*math.cos(t),0,r*.84*math.sin(t)];sz=(r*.30,thick,r*.20);R=rot('y',-t)
   self.box(sz,p,1,group,R)

def build():
 a=Asset('vf_shaft_2m','Timber shaft · 2 m');g=a.group('Rotor',[0,.5,0],'z')
 a.cyl(.10,2,(0,0,0),0,g,'z',8)
 for z in [-.75,.75]:
  a.box((.42,.12,.32),(0,.08,z));a.box((.26,.38,.22),(0,.29,z));a.cyl(.16,.18,(0,.5,z),2,axis='z',inner=.11)
  a.cyl(.13,.06,(0,0,z),2,g,'z')
 a.port('Kinetic_In',[0,.5,-1],[0,0,-1],'kinetic');a.port('Kinetic_Out',[0,.5,1],[0,0,1],'kinetic');a.collider('Base',[.42,.6,2],[0,.3,0])
 a=Asset('vf_cog','Bronze-braced cog');g=a.group('Rotor',[0,.68,0],'x');a.gear(.58,.18,(0,0,0),g);a.cyl(.10,.70,(0,0,0),2,g,'x')
 a.box((.90,.12,.55),(0,.06,0))
 for x in [-.30,.30]:a.box((.12,.64,.15),(x,.36,0));a.cyl(.14,.12,(x,.68,0),3,axis='x',inner=.105)
 a.port('Kinetic_In',[-.35,.68,0],[-1,0,0],'kinetic');a.port('Kinetic_Out',[.35,.68,0],[1,0,0],'kinetic');a.collider('Stand',[.9,.65,.55],[0,.325,0])
 a=Asset('vf_hand_crank','Hand crank');a.box((.7,.16,.65),(0,.08,0));a.box((.22,.95,.22),(0,.63,0));a.cyl(.20,.30,(0,1.05,0),3,axis='x',inner=.10)
 g=a.group('Crank',[0,1.05,0],'x');a.cyl(.09,.75,(0,0,0),2,g,'x');a.beam((.4,0,0),(.4,.38,0),.10,mat=2,group=g);a.cyl(.075,.28,(.55,.38,0),0,g,'x')
 a.port('Kinetic_Out',[-.375,1.05,0],[-1,0,0],'kinetic');a.port('Interact',[.72,1.43,0],[1,0,0],'service');a.collider('Stand',[.7,1.1,.65],[0,.55,0])
 a=Asset('vf_water_wheel','Water wheel');a.feet(1.9,1.6,2.0)
 for x in [-.95,.95]:
  a.box((.26,.22,2.0),(x,2.0,0));a.beam((x,.3,-.8),(x,1.9,.5),.14);a.beam((x,.3,.8),(x,1.9,-.5),.14)
 g=a.group('Rotor',[0,2.0,0],'x');a.cyl(.16,2.5,(0,0,0),2,g,'x')
 for x in [-.55,.55]:
  a.cyl(1.68,.12,(x,0,0),0,g,'x',24,1.50);a.cyl(1.72,.045,(x,0,0),2,g,'x',24,1.66)
  for i in range(8):
   t=i*math.pi/4;a.beam((x,0,0),(x,1.55*math.cos(t),1.55*math.sin(t)),.13,mat=1,group=g)
 a.cyl(.31,1.3,(0,0,0),0,g,'x')
 for i in range(16):
  t=2*math.pi*i/16;a.box((1.26,.14,.40),(0,1.64*math.cos(t),1.64*math.sin(t)),1,g,rot('x',t))
 for x in [-.95,.95]:a.cyl(.25,.20,(x,2,0),3,axis='x',inner=.17)
 a.port('Kinetic_Left',[-1.25,2,0],[-1,0,0],'kinetic');a.port('Kinetic_Right',[1.25,2,0],[1,0,0],'kinetic');a.port('Water_Probe',[0,.35,0],[0,-1,0],'environment')
 for x in [-.95,.95]:a.collider('Support_'+str(x),[.3,2.15,1.95],[x,1.075,0])
 a=Asset('vf_conveyor_2m','Timber conveyor · 2 m');a.feet(.92,1.6,.65)
 for x in [-.49,.49]:a.box((.12,.16,2),(x,.68,0));a.box((.055,.12,2),(x,.89,0),2)
 g=a.group('Belt_Surface',[0,.77,0],None)
 for z in np.linspace(-.94,.94,16):a.box((.91,.07,.10),(0,0,z),1,g)
 for z in [-.86,.86]:
  r=a.group('Roller_'+str(z),[0,.70,z],'x');a.cyl(.12,1.08,(0,0,0),2,r,'x')
 a.port('Item_In',[0,.82,-1],[0,0,-1],'item_in');a.port('Item_Out',[0,.82,1],[0,0,1],'item_out');a.port('Kinetic',[.58,.70,-.86],[1,0,0],'kinetic');a.collider('Deck',[1.1,.25,2],[0,.73,0])
 a=Asset('vf_conveyor_corner','Timber conveyor · corner');a.feet(.9,.9,.65)
 for i in range(9):
  t=i*math.pi/16;p=(-.55+.76*math.cos(t),.77,-.55+.76*math.sin(t));a.box((.82,.08,.10),p,1,rotation=rot('y',-t))
 for radius in [.34,1.18]:
  for i in range(8):
   t=i*math.pi/16;u=(i+1)*math.pi/16;a.beam((-.55+radius*math.cos(t),.90,-.55+radius*math.sin(t)),(-.55+radius*math.cos(u),.90,-.55+radius*math.sin(u)),.06,mat=2)
 a.port('Item_In',[.21,.82,-.60],[0,0,-1],'item_in');a.port('Item_Out',[-.60,.82,.21],[-1,0,0],'item_out');a.port('Kinetic',[.65,.65,0],[1,0,0],'kinetic');a.collider('Deck',[1.3,.22,1.3],[.05,.76,.05])
 a=Asset('vf_splitter','Three-way splitter');a.feet(1.05,1.05,.68);a.box((1.3,.16,1.3),(0,.70,0));a.box((1.14,.06,1.14),(0,.81,0),4)
 g=a.group('Selector',[0,.87,0],'y');a.cyl(.30,.10,(0,0,0),3,g);a.box((.12,.12,.85),(0,.03,0),1,g)
 for x in [-.60,.60]:
  for z in [-.60,.60]:a.box((.08,.25,.08),(x,.92,z),2)
 for name,p,d,k in [('Item_In',[0,.85,-.7],[0,0,-1],'item_in'),('Item_Out_Left',[-.7,.85,0],[-1,0,0],'item_out'),('Item_Out_Right',[.7,.85,0],[1,0,0],'item_out'),('Item_Out_Forward',[0,.85,.7],[0,0,1],'item_out'),('Kinetic',[0,.52,.70],[0,0,1],'kinetic')]:a.port(name,p,d,k)
 a.collider('Body',[1.3,.25,1.3],[0,.75,0])
 a=Asset('vf_feeder','Bronze feeder');a.box((.8,.16,.8),(0,.08,0));a.box((.30,.57,.30),(0,.43,0));a.cyl(.23,.12,(0,.73,0),3)
 g=a.group('Arm_Yaw',[0,.8,0],'y');a.cyl(.18,.18,(0,0,0),2,g);a.beam((0,.06,0),(0,.34,.63),.13,mat=0,group=g);a.beam((0,.34,.63),(0,.10,1.08),.10,mat=2,group=g)
 a.box((.36,.08,.12),(0,.08,1.08),3,g)
 for x in [-.16,.16]:a.box((.065,.23,.10),(x,-.03,1.08),2,g)
 a.port('Pickup',[0,.68,1.08],[0,0,1],'item_in');a.port('Dropoff',[0,.68,-1.08],[0,0,-1],'item_out');a.port('Kinetic',[.40,.40,0],[1,0,0],'kinetic');a.collider('Base',[.8,.8,.8],[0,.4,0])
 a=Asset('vf_catch_basket','Catch basket');a.box((1.0,.10,.9),(0,.05,0),0)
 for y in [.20,.42,.64]:
  for x in [-.48,.48]:a.box((.065,.15,.94),(x,y,0),1)
  for z in [-.43,.43]:a.box((.95,.15,.065),(0,y,z),1)
 for x in [-.44,.44]:
  for z in [-.39,.39]:a.box((.09,.78,.09),(x,.39,z),0)
 for y in [.14,.69]:
  for x in [-.51,.51]:a.box((.03,.045,1),(x,y,0),2)
  for z in [-.47,.47]:a.box((1.02,.045,.03),(0,y,z),2)
 a.port('Item_In',[0,.82,0],[0,1,0],'item_in');a.port('Item_Out',[0,.35,.5],[0,0,1],'item_out')
 for x in [-.48,.48]:a.collider('Side'+str(x),[.10,.75,.95],[x,.4,0])
 for z in [-.43,.43]:a.collider('End'+str(z),[.95,.75,.10],[0,.4,z])
 a.collider('Floor',[1,.10,.9],[0,.05,0])
 a=Asset('vf_gravity_trough','Gravity trough');slope=-.20
 for x in [-.43,.43]:a.box((.10,.32,2.04),(x,.75,0),0,rotation=rot('x',slope))
 a.box((.84,.09,2.04),(0,.62,0),1,rotation=rot('x',slope))
 for z,h in [(-.8,.45),(.8,.78)]:
  # upper end at +Z, lower at -Z
  for x in [-.3,.3]:a.box((.12,h,.12),(x,h/2,z))
 a.port('Item_In',[0,.94,1],[0,0,1],'item_in');a.port('Item_Out',[0,.53,-1],[0,0,-1],'item_out');a.collider('Body',[.96,.55,2.06],[0,.67,0])
 a=Asset('vf_recipe_mill','Recipe mill');a.feet(1.35,1.10,1.0);a.box((1.65,.18,1.4),(0,1.05,0));a.cyl(.58,.18,(0,1.23,0),5,n=20)
 g=a.group('Millstone',[0,1.43,0],'y');a.cyl(.54,.22,(0,0,0),5,g,n=20);a.cyl(.13,.38,(0,.08,0),3,g)
 for x in [-.67,.67]:a.box((.15,.85,.15),(x,1.55,0))
 a.box((1.55,.16,.2),(0,1.94,0));a.cyl(.07,.75,(0,1.82,0),2)
 a.box((.48,.25,.55),(0,1.62,-.58),0);a.box((.4,.08,.70),(0,1.02,.83),1)
 for i,(p,d) in enumerate([([-.84,1.13,0],[-1,0,0]),([.84,1.13,0],[1,0,0]),([0,1.13,-.74],[0,0,-1])]):a.port('Item_In_'+str(i),p,d,'item_in')
 a.port('Item_In_3',[0,1.70,-.87],[0,0,-1],'item_in');a.port('Item_Out',[0,1.06,1.18],[0,0,1],'item_out');a.port('Kinetic',[.86,.65,0],[1,0,0],'kinetic');a.collider('Table',[1.65,1.15,1.4],[0,.575,0])
 a=Asset('vf_quarry','Bedrock quarry');a.box((2.2,.22,2.0),(0,.11,0),5)
 for x in [-.80,.80]:
  for z in [-.65,.65]:a.box((.22,2.4,.22),(x,1.4,z));a.box((.27,.12,.27),(x,.36,z),2);a.box((.27,.12,.27),(x,2.40,z),2)
 for z in [-.65,.65]:a.box((1.95,.23,.23),(0,2.56,z))
 a.beam((-.8,.45,-.65),(.8,2.35,-.65),.13)
 g=a.group('Drill',[0,1.25,0],'y');a.cyl(.13,2.25,(0,0,0),2,g)
 # segmented helix, open geometry between flights intentionally visible
 for i in range(24):
  t=i*math.pi/6;y=-.75+i*.060;a.box((.48,.06,.13),(0,y,0),2,g,rot('y',t))
 a.cyl(.34,.22,(0,1.02,0),3,g);a.box((.48,.10,.9),(0,.44,.90),1)
 a.port('Item_Out',[0,.49,1.35],[0,0,1],'item_out');a.port('Kinetic',[1.05,2.35,0],[1,0,0],'kinetic');a.port('Ground_Probe',[0,0,0],[0,-1,0],'environment')
 for x in [-.8,.8]:a.collider('Side'+str(x),[.26,2.65,1.55],[x,1.325,0])
 a.collider('Base',[2.2,.22,2],[0,.11,0])

def export(a):
 modeldir=ROOT/'models'/a.name;modeldir.mkdir(parents=True,exist_ok=True)
 nodes=[{'name':a.name,'children':[],'extras':{'units':'metres','version':'0.1.0'}}];meshes=[];views=[];access=[];blob=bytearray();allworld=[];obj=['# Y-up metres. Static fallback: pivots/markers are in asset.json.','mtllib materials.mtl'];oi=1;tri_count=0
 def acc(arr,typ):
  while len(blob)%4:blob.append(0)
  arr=np.array(arr,dtype='<f4');start=len(blob);raw=arr.tobytes();blob.extend(raw);views.append({'buffer':0,'byteOffset':start,'byteLength':len(raw),'target':34962});entry={'bufferView':len(views)-1,'componentType':5126,'count':len(arr),'type':typ}
  if typ=='VEC3':entry.update(min=arr.min(axis=0).tolist(),max=arr.max(axis=0).tolist())
  access.append(entry);return len(access)-1
 for gn,g in a.groups.items():
  primitives=[]
  for mat in range(len(MATS)):
   verts=[];norms=[];uvs=[]
   for v,f,m in g['parts']:
    if m!=mat:continue
    t=v[f];n=np.cross(t[:,1]-t[:,0],t[:,2]-t[:,0]);length=np.linalg.norm(n,axis=1);assert np.all(length>1e-9)
    n/=length[:,None];verts.extend(t.reshape(-1,3));norms.extend(np.repeat(n,3,axis=0));uvs.extend(np.tile([[0,0],[1,0],[1,1]],(len(f),1)));tri_count+=len(f)
   if not verts:continue
   v=np.array(verts);world=v+g['pivot'];allworld.extend(world)
   primitives.append({'attributes':{'POSITION':acc(v,'VEC3'),'NORMAL':acc(norms,'VEC3'),'TEXCOORD_0':acc(uvs,'VEC2')},'material':mat,'mode':4})
   obj.extend(['o '+gn+'_'+MATS[mat][0],'usemtl '+MATS[mat][0]])
   obj.extend('v %.6f %.6f %.6f'%tuple(p) for p in world)
   for i in range(0,len(v),3):obj.append(f'f {oi+i} {oi+i+1} {oi+i+2}')
   oi+=len(v)
  if primitives:
   meshes.append({'name':gn,'primitives':primitives});idx=len(nodes);nodes.append({'name':gn,'mesh':len(meshes)-1,'translation':g['pivot'],'extras':{'rotation_axis':g['axis'] or 'none'}});nodes[0]['children'].append(idx)
 for p in a.markers:
  nodes[0]['children'].append(len(nodes));nodes.append({'name':p['name'],'translation':p['position'],'extras':{'kind':p['kind'],'forward':p['forward']}})
 materials=[{'name':n,'pbrMetallicRoughness':{'baseColorFactor':c,'metallicFactor':m,'roughnessFactor':r}} for n,c,m,r in MATS]
 data={'asset':{'version':'2.0','generator':'VikingFactory original procedural prototype generator'},'scene':0,'scenes':[{'nodes':[0]}],'nodes':nodes,'meshes':meshes,'materials':materials,'buffers':[{'byteLength':len(blob)}],'bufferViews':views,'accessors':access}
 js=json.dumps(data,separators=(',',':')).encode();js+=b' '*((-len(js))%4);blob+=b'\x00'*((-len(blob))%4)
 glb=struct.pack('<4sII',b'glTF',2,12+8+len(js)+8+len(blob))+struct.pack('<I4s',len(js),b'JSON')+js+struct.pack('<I4s',len(blob),b'BIN\x00')+blob
 (modeldir/(a.name+'.glb')).write_bytes(glb);(modeldir/(a.name+'.obj')).write_text('\n'.join(obj)+'\n')
 (modeldir/'materials.mtl').write_text('\n'.join(f'newmtl {n}\nKd {c[0]} {c[1]} {c[2]}\nKs {m*.3} {m*.3} {m*.3}\nNs 20\n' for n,c,m,r in MATS))
 bounds=np.array([np.min(allworld,axis=0),np.max(allworld,axis=0)])
 manifest={'id':a.name,'label':a.label,'units':'metres','coordinates':'right-handed Y-up; positions in authoring coordinates, let importer convert','triangles':tri_count,'bounds':bounds.tolist(),'parts':[{'name':k,'pivot':v['pivot'],'rotation_axis':v['axis']} for k,v in a.groups.items()],'markers':a.markers,'suggested_colliders':a.colliders,'status':'prototype; not a Unity prefab or game-tested asset'}
 (modeldir/'asset.json').write_text(json.dumps(manifest,indent=2));return manifest

def previews():
 import matplotlib;matplotlib.use('Agg')
 import matplotlib.pyplot as plt
 from mpl_toolkits.mplot3d.art3d import Poly3DCollection
 fig=plt.figure(figsize=(16,18),facecolor='#171e20')
 light=np.array([-.4,.8,.5]);light/=np.linalg.norm(light)
 for idx,a in enumerate(ASSETS):
  ax=fig.add_subplot(4,3,idx+1,projection='3d',computed_zorder=False);ax.set_facecolor('#171e20');pts=[]
  for g in a.groups.values():
   for v,f,m in g['parts']:
    vv=v+g['pivot'];pts.extend(vv);tri=vv[f];norm=np.cross(tri[:,1]-tri[:,0],tri[:,2]-tri[:,0]);norm/=np.linalg.norm(norm,axis=1)[:,None];shade=.52+.48*np.maximum(0,norm@light);base=np.array(MATS[m][1][:3]);colors=np.clip(base[None,:]*shade[:,None]*1.6,0,1)
    # matplotlib Z-up coordinates
    ax.add_collection3d(Poly3DCollection(tri[:,:,[0,2,1]],facecolors=colors,edgecolors=(0,0,0,.08),linewidths=.18,zsort='average'))
  pts=np.array(pts);lo=pts.min(0);hi=pts.max(0);c=(lo+hi)/2;span=max(hi-lo)*.63
  ax.set_xlim(c[0]-span,c[0]+span);ax.set_ylim(c[2]-span,c[2]+span);ax.set_zlim(c[1]-span,c[1]+span);ax.set_box_aspect((1,1,1));ax.view_init(22,38);ax.set_proj_type('ortho');ax.set_axis_off();ax.set_title(a.label,color='#e7d7b8',fontsize=12,pad=-8)
 fig.suptitle('VIKINGFACTORY  /  WORKSHOP ASSET KIT',color='#eee1c8',fontsize=23,y=.975,weight='bold');fig.text(.5,.947,'12 original modular models · timber, bronze & iron · prototype v0.1',ha='center',color='#9caeac',fontsize=12);fig.text(.5,.018,'Preview rendered from the supplied mesh geometry. Materials are flat-color prototypes.',ha='center',color='#9caeac',fontsize=10)
 plt.subplots_adjust(top=.92,bottom=.04,hspace=.08,wspace=.02);fig.savefig(ROOT/'previews'/'Workshop-Lineup.png',dpi=130,facecolor=fig.get_facecolor());plt.close(fig)
if __name__=='__main__':
 build();out=[export(a) for a in ASSETS];(ROOT/'catalog.json').write_text(json.dumps({'version':'0.1.0','assets':out},indent=2));print('Exported',len(out),'assets;',sum(x['triangles'] for x in out),'triangles total')
 try:previews();print('Preview rendered')
 except ImportError:print('matplotlib absent: skipping optional preview')
