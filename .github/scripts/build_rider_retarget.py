import bpy
import os
from mathutils import Vector, Matrix

ROOT = os.getcwd()
SRC_FBX = os.path.join(ROOT, "Assets/ExternalAsset/Floreswa/Models/male01_1.fbx")
RIG_FBX = os.path.join(ROOT, "Assets/ExternalAsset/RayznGames/BicycleSystem/URP/Models/Rag_Rig_Model_URP.fbx")
OUT_DIR = os.path.join(ROOT, "Assets/JEC/Generated/RiderRetarget")
OUT_FBX = os.path.join(OUT_DIR, "ParkourBikeRider_RagRig.fbx")
OUT_PNG = os.path.join(OUT_DIR, "ParkourBikeRider_preview.png")
os.makedirs(OUT_DIR, exist_ok=True)

def clear_scene():
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.object.delete(use_global=False)

def import_fbx(path):
    before = set(bpy.data.objects)
    bpy.ops.import_scene.fbx(filepath=path, use_anim=False, automatic_bone_orientation=False)
    return list(set(bpy.data.objects) - before)

def choose_armature(objs):
    arms = [o for o in objs if o.type == 'ARMATURE']
    if not arms: raise RuntimeError(f"No armature found in {objs}")
    return max(arms, key=lambda o: len(o.data.bones))

def choose_mesh(objs, armature=None):
    meshes = [o for o in objs if o.type == 'MESH']
    if armature:
        skinned = [o for o in meshes if any(m.type == 'ARMATURE' and m.object == armature for m in o.modifiers)]
        if skinned: meshes = skinned
    if not meshes: raise RuntimeError("No mesh found")
    return max(meshes, key=lambda o: len(o.data.vertices))

def world_bbox(obj):
    pts = [obj.matrix_world @ Vector(c) for c in obj.bound_box]
    mn = Vector((min(p.x for p in pts), min(p.y for p in pts), min(p.z for p in pts)))
    mx = Vector((max(p.x for p in pts), max(p.y for p in pts), max(p.z for p in pts)))
    return mn, mx

def copy_mesh_in_world(src):
    mesh = src.data.copy()
    obj = bpy.data.objects.new("ParkourBikeRider_Mesh", mesh)
    bpy.context.collection.objects.link(obj)
    mw = src.matrix_world.copy()
    for v in mesh.vertices: v.co = mw @ v.co
    obj.matrix_world = Matrix.Identity(4)
    return obj

def align_source_to_target(src, tgt):
    smin, smax = world_bbox(src); tmin, tmax = world_bbox(tgt)
    ssize = smax - smin; tsize = tmax - tmin
    base = tsize.z / max(ssize.z, 1e-6)
    sx = base * max(0.88, min(1.12, tsize.x / max(ssize.x * base, 1e-6)))
    sy = base * max(0.88, min(1.12, tsize.y / max(ssize.y * base, 1e-6)))
    sz = base
    smid = (smin + smax) * 0.5; tmid = (tmin + tmax) * 0.5
    scale_m = Matrix.Diagonal(Vector((sx, sy, sz, 1.0)))
    scaled_mid = scale_m @ smid
    offset = Vector((tmid.x - scaled_mid.x, tmid.y - scaled_mid.y, tmin.z - (scale_m @ smin).z))
    xform = Matrix.Translation(offset) @ scale_m
    for v in src.data.vertices: v.co = xform @ v.co
    return sx, sy, sz

def transfer_weights(dst, src):
    for vg in src.vertex_groups:
        if dst.vertex_groups.get(vg.name) is None: dst.vertex_groups.new(name=vg.name)
    mod = dst.modifiers.new("Transfer_RagRig_Weights", 'DATA_TRANSFER')
    mod.object = src
    mod.use_vert_data = True
    mod.data_types_verts = {'VGROUP_WEIGHTS'}
    mod.vert_mapping = 'POLYINTERP_NEAREST'
    mod.layers_vgroup_select_src = 'ALL'
    mod.layers_vgroup_select_dst = 'NAME'
    mod.mix_mode = 'REPLACE'
    mod.mix_factor = 1.0
    bpy.context.view_layer.objects.active = dst
    dst.select_set(True)
    bpy.ops.object.modifier_apply(modifier=mod.name)

def bind_to_armature(mesh, arm):
    mesh.parent = arm
    mesh.matrix_parent_inverse = arm.matrix_world.inverted()
    mod = mesh.modifiers.get("Armature") or mesh.modifiers.new("Armature", 'ARMATURE')
    mod.object = arm
    mod.use_deform_preserve_volume = True

def remove_non_deform_groups(mesh, arm):
    deform = {b.name for b in arm.data.bones if b.use_deform}
    for vg in list(mesh.vertex_groups):
        if vg.name not in deform: mesh.vertex_groups.remove(vg)

def normalize_weights(mesh):
    bpy.context.view_layer.objects.active = mesh
    bpy.ops.object.mode_set(mode='OBJECT')
    mesh.select_set(True)
    try: bpy.ops.object.vertex_group_normalize_all(lock_active=False)
    except Exception as exc: print("normalize warning:", exc)

def clean_objects(keep):
    keep = set(keep)
    for obj in list(bpy.data.objects):
        if obj not in keep: bpy.data.objects.remove(obj, do_unlink=True)

def export_fbx(mesh, arm):
    bpy.ops.object.select_all(action='DESELECT')
    mesh.select_set(True); arm.select_set(True); bpy.context.view_layer.objects.active = arm
    arm.data.pose_position = 'REST'
    bpy.ops.export_scene.fbx(filepath=OUT_FBX, use_selection=True, object_types={'ARMATURE','MESH'}, apply_unit_scale=True, apply_scale_options='FBX_SCALE_ALL', bake_space_transform=False, use_mesh_modifiers=True, mesh_smooth_type='FACE', use_subsurf=False, use_mesh_edges=False, use_tspace=False, add_leaf_bones=False, primary_bone_axis='Y', secondary_bone_axis='X', use_armature_deform_only=False, armature_nodetype='NULL', bake_anim=False, path_mode='COPY', embed_textures=True, axis_forward='-Z', axis_up='Y')

def setup_render(mesh, arm):
    scene = bpy.context.scene
    try: scene.render.engine = 'BLENDER_EEVEE_NEXT'
    except Exception: scene.render.engine = 'BLENDER_EEVEE'
    scene.render.resolution_x = 900; scene.render.resolution_y = 1100; scene.render.resolution_percentage = 100
    scene.render.film_transparent = True; scene.render.image_settings.file_format = 'PNG'; scene.render.filepath = OUT_PNG
    arm.data.pose_position = 'REST'
    cam_data = bpy.data.cameras.new("PreviewCamera"); cam = bpy.data.objects.new("PreviewCamera", cam_data); scene.collection.objects.link(cam); scene.camera = cam
    mn, mx = world_bbox(mesh); center = (mn + mx) * 0.5; height = max(mx.z - mn.z, 0.5); width = max(mx.x - mn.x, 0.4); radius = max(height, width) * 0.55
    cam.location = center + Vector((radius * 2.15, -radius * 2.8, radius * 0.25)); cam.rotation_euler = (center - cam.location).to_track_quat('-Z','Y').to_euler(); cam.data.lens = 55
    for name, energy, size, offset in [("Key",1100,2.2,(2.3,-2.0,2.6)),("Fill",550,2.0,(-2.0,-0.5,1.2)),("Rim",700,1.4,(0,2.4,1.8))]:
        data = bpy.data.lights.new(name, type='AREA'); data.energy = energy; data.size = radius * size
        light = bpy.data.objects.new(name, data); scene.collection.objects.link(light); light.location = center + Vector(tuple(radius * x for x in offset)); light.rotation_euler = (center - light.location).to_track_quat('-Z','Y').to_euler()
    scene.world.color = (0.035,0.035,0.035)
    bpy.ops.render.render(write_still=True)

def main():
    clear_scene()
    target_objs = import_fbx(RIG_FBX); target_arm = choose_armature(target_objs); target_mesh = choose_mesh(target_objs, target_arm); target_arm.data.pose_position = 'REST'
    target_arm.name = "Rag_Rig_URP"; target_arm.data.name = "Rag_Rig_URP_Armature"
    source_objs = import_fbx(SRC_FBX); source_arm = choose_armature(source_objs); source_mesh = choose_mesh(source_objs, source_arm); source_arm.data.pose_position = 'REST'
    rider = copy_mesh_in_world(source_mesh); print("alignment:", align_source_to_target(rider, target_mesh))
    transfer_weights(rider, target_mesh); remove_non_deform_groups(rider, target_arm); bind_to_armature(rider, target_arm); normalize_weights(rider)
    rider.name = "ParkourBikeRider_Mesh"; rider.data.name = "ParkourBikeRider_Mesh"
    clean_objects([target_arm, rider]); export_fbx(rider, target_arm); setup_render(rider, target_arm)
    print("Generated", OUT_FBX); print("Generated", OUT_PNG)

if __name__ == "__main__": main()
