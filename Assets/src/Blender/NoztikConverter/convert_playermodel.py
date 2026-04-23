
# Use BlenderSourceTools to import your .smd files into the project
# http://steamreview.org/BlenderSourceTools/

# Parent all the meshes to a single skeleton and put them into the Input collection
# Then press the "Run Script" button in the top middle of this window
# This will automatically rig your meshes to the new skeleton and export it as a .fbx file







































import bpy
import mathutils
import math
import os

bone_rename_table = {
	"ValveBiped.Bip01_Pelvis": "pelvis",
	#"ValveBiped.Bip01_Spine": "spine_0",
	#"ValveBiped.Bip01_Spine1": "spine_1",
	#"ValveBiped.Bip01_Spine2": "spine_1",
	#"ValveBiped.Bip01_Spine4": "spine_2",
	
	"ValveBiped.Bip01_Neck1": "neck_0",
	"ValveBiped.Bip01_Head1": "head",
	"ValveBiped.Bip01_R_Eye": "eye_R",
	"ValveBiped.Bip01_L_Eye": "eye_L",
	"Eye_R": "eye_R",
	"Eye_L": "eye_L",




	"ValveBiped.Bip01_R_Clavicle": "clavicle_R",
	
	"ValveBiped.Bip01_R_UpperArm": "arm_upper_R",
	#"ValveBiped.Bip01_R_Trapezius": "", # shoulder twist bone
	
	"ValveBiped.Bip01_R_Shoulder": "arm_upper_R_twist0",
	
	"ValveBiped.Bip01_R_Forearm_Helperbone": "arm_upper_R_twist1",
	"ValveBiped.Bip01_R_Bicep": "arm_upper_R_twist1",
	
	"ValveBiped.Bip01_R_Elbow": "arm_elbow_helper_R",
	
	"ValveBiped.Bip01_R_Forearm": "arm_lower_R",
	
	"ValveBiped.Bip01_R_Hand_Helperbone": "arm_lower_R_twist1",
	"ValveBiped.Bip01_R_Ulna": "arm_lower_R_twist1",


	#"ValveBiped.Bip01_R_Wrist": "", # wrist twist bone
	"ValveBiped.Bip01_R_Hand": "hand_R",
	
	"ValveBiped.Bip01_R_Finger0": "finger_thumb_0_R",
	"ValveBiped.Bip01_R_Finger01": "finger_thumb_1_R",
	"ValveBiped.Bip01_R_Finger02": "finger_thumb_2_R",
	
	"ValveBiped.Bip01_R_Finger1": "finger_index_0_R",
	"ValveBiped.Bip01_R_Finger11": "finger_index_1_R",
	"ValveBiped.Bip01_R_Finger12": "finger_index_2_R",
	
	"ValveBiped.Bip01_R_Finger2": "finger_middle_0_R",
	"ValveBiped.Bip01_R_Finger21": "finger_middle_1_R",
	"ValveBiped.Bip01_R_Finger22": "finger_middle_2_R",
	
	"ValveBiped.Bip01_R_Finger3": "finger_ring_0_R",
	"ValveBiped.Bip01_R_Finger31": "finger_ring_1_R",
	"ValveBiped.Bip01_R_Finger32": "finger_ring_2_R",
	
	"ValveBiped.Bip01_R_Finger4": "finger_pinky_0_R",
	"ValveBiped.Bip01_R_Finger41": "finger_pinky_1_R",
	"ValveBiped.Bip01_R_Finger42": "finger_pinky_2_R",




	"ValveBiped.Bip01_L_Clavicle": "clavicle_L",
	
	"ValveBiped.Bip01_L_UpperArm": "arm_upper_L",
	#"ValveBiped.Bip01_L_Trapezius": "", # shoulder twist bone
	
	"ValveBiped.Bip01_L_Shoulder": "arm_upper_L_twist0",
	
	"ValveBiped.Bip01_L_Forearm_Helperbone": "arm_upper_L_twist1",
	"ValveBiped.Bip01_L_Bicep": "arm_upper_L_twist1",
	
	"ValveBiped.Bip01_L_Elbow": "arm_elbow_helper_L",
	
	"ValveBiped.Bip01_L_Forearm": "arm_lower_L",
	
	"ValveBiped.Bip01_L_Hand_Helperbone": "arm_lower_L_twist1",
	"ValveBiped.Bip01_L_Ulna": "arm_lower_L_twist1",


	#"ValveBiped.Bip01_L_Wrist": "", # wrist twist bone
	"ValveBiped.Bip01_L_Hand": "hand_L",
	
	"ValveBiped.Bip01_L_Finger0": "finger_thumb_0_L",
	"ValveBiped.Bip01_L_Finger01": "finger_thumb_1_L",
	"ValveBiped.Bip01_L_Finger02": "finger_thumb_2_L",
	
	"ValveBiped.Bip01_L_Finger1": "finger_index_0_L",
	"ValveBiped.Bip01_L_Finger11": "finger_index_1_L",
	"ValveBiped.Bip01_L_Finger12": "finger_index_2_L",
	
	"ValveBiped.Bip01_L_Finger2": "finger_middle_0_L",
	"ValveBiped.Bip01_L_Finger21": "finger_middle_1_L",
	"ValveBiped.Bip01_L_Finger22": "finger_middle_2_L",
	
	"ValveBiped.Bip01_L_Finger3": "finger_ring_0_L",
	"ValveBiped.Bip01_L_Finger31": "finger_ring_1_L",
	"ValveBiped.Bip01_L_Finger32": "finger_ring_2_L",
	
	"ValveBiped.Bip01_L_Finger4": "finger_pinky_0_L",
	"ValveBiped.Bip01_L_Finger41": "finger_pinky_1_L",
	"ValveBiped.Bip01_L_Finger42": "finger_pinky_2_L",




	"ValveBiped.Bip01_L_Thigh": "leg_upper_L",
	"ValveBiped.Bip01_L_Calf": "leg_lower_L",
	"ValveBiped.Bip01_L_Foot": "ankle_L",
	"ValveBiped.Bip01_L_Toe0": "ball_L",
	
	"ValveBiped.Bip01_R_Thigh": "leg_upper_R",
	"ValveBiped.Bip01_R_Calf": "leg_lower_R",
	"ValveBiped.Bip01_R_Foot": "ankle_R",
	"ValveBiped.Bip01_R_Toe0": "ball_R",
}

protected_bones = [
	"root_IK",
	"aim_matrix_01",
	"aim_matrix_02a",
	"aim_matrix_02b",
	"foot_R_IK_target",
	"foot_L_IK_target",
	"hand_R_IK_attach",
	"hand_R_IK_target",
	"hand_L_IK_attach",
	"hand_L_IK_target",
	"hand_R_to_L_ikrule",
	"hand_L_to_R_ikrule",
	"hold_L",
	"hold_R",
]



def createMidpointBone(skeleton, mesh, parent, child, name, middle=True):
	new_bone = skeleton.new(name)
	new_bone.head = child.head.copy()
	new_bone.tail = child.tail.copy()
	new_bone.parent = parent
	
	if middle:
		child.parent = new_bone
	
	head_midpoint = (parent.head + child.head) / 2
	tail_midpoint = (parent.tail + child.tail) / 2
	new_bone.head = head_midpoint
	new_bone.tail = tail_midpoint

	vg = mesh.vertex_groups.get(name)
	if not vg:
		print(f"Creating vertex group: {name}")
		mesh.vertex_groups.new(name=name)

def disconnectBones(arm):
	bpy.context.view_layer.objects.active = arm
	bpy.ops.object.mode_set(mode="EDIT")
	for bone in arm.data.edit_bones:
		if bone.name not in protected_bones and bone.use_connect:
			bone.use_connect = False





# 1. Get the armatures and meshes from the Input collection
# 2. Handle the different number of possible spine bones
# 3. Rename Input armature's bones and vertex groups to Human Citizen bones and vertex groups
# 4. Create metacarpal bones in Input armature
# 5. Create missing joint helper bones in Input armature
# 6. Create twist bones in arms and legs in Input armature
# 7. Create any missing bones in the Human Citizen armature
# 8. Move the Human Citizen armature's bones to match the Input armature in edit mode
# 9. Move the Input meshes to the Human Citizen armature
# 10. Weight paint the metacarpals in the Human Citizen armature
# 11. Pose the Human Citizen armature to match the reference armature shape
# 12. Realign miscellaneous bones in the Human Citizen armature
# 13. Export the Human Citizen armature and meshes
# 14. Cleanup


# Export Instructions
	# Primary Bone Axis: X Axis
	# Secondary Bone Axis: Z Axis
	# Add Leaf Bones: False



# enable moving eyes
EYES_ENABLED = True







# 1. Get the armatures and meshes from the Input collection

meshes = []
for obj in bpy.data.collections["Input"].all_objects:
	print(f"{obj.name}: {obj.type}")
	if obj.type == "ARMATURE":
		obj.hide_set(False)
		arm_a = obj
		continue
	
	if obj.type == "MESH" and not obj.name.endswith("_physics"):
		mods = [m for m in obj.modifiers if m.type == "ARMATURE"]
		if not mods:
			Exception(f"{obj.name} doesn't have an armature modifier")
		if not mods[0].object:
			Exception(f"{obj.name}'s armature modifier isn't pointing to an armature")

		obj.hide_set(False)
		if obj.data.shape_keys:
			obj.shape_key_clear()
		meshes.append(obj)
		continue



if not arm_a:
	raise Exception(f"No armature in Input found")

ARMATURE_B = "Human Citizen Armature"
arm_b = bpy.data.objects.get(ARMATURE_B)
if not arm_b:
	raise Exception(f"Armature \"{ARMATURE_B}\" not found")

if not meshes:
	raise Exception(f"No meshes in Input found")




# disconnect any connected bones in the Human Citizen armature
disconnectBones(arm_b)



bpy.ops.object.mode_set(mode="OBJECT")
bpy.ops.object.select_all(action="DESELECT")


# 2. Handle the different number of possible spine bones

arm_a.select_set(True)
bpy.ops.object.mode_set(mode="EDIT")
bpy.ops.armature.select_all(action="DESELECT")
bones_a = arm_a.data.edit_bones

spine_bones = sorted([b.name for b in arm_a.data.edit_bones if b.name.startswith("ValveBiped.Bip01_Spine")])

if len(spine_bones) > 3:
	if (bones_a.get("ValveBiped.Bip01_Spine1") and bones_a.get("ValveBiped.Bip01_Spine2")):
		# share Bip01_Spine1's weight paint between Bip01_Spine and Bip01_Spine2
		for mesh in meshes:
			vg_bone = mesh.vertex_groups.get("ValveBiped.Bip01_Spine1")
			vg_parent = mesh.vertex_groups.get("ValveBiped.Bip01_Spine")
			vg_child = mesh.vertex_groups.get("ValveBiped.Bip01_Spine2")
	
			for v in mesh.data.vertices:
				try:
					w = vg_bone.weight(v.index)
				except RuntimeError:
					w = 0.0
	
				if w > 0:
					vg_parent.add([v.index], w * 0.5, "ADD")
					vg_child.add([v.index], w * 0.5, "ADD")
	
		# merge the two middle spine bones together
		print("Merging ValveBiped.Bip01_Spine1 and ValveBiped.Bip01_Spine2")
		bones_a.get("ValveBiped.Bip01_Spine").parent = bones_a.get("ValveBiped.Bip01_Spine2")
		bones_a.remove(bones_a.get("ValveBiped.Bip01_Spine1"))



spine_counter = 0
bones_a = arm_a.data.edit_bones

# rename the Input armature's spine bones and vertex groups in numerical order
for spine_bone in spine_bones:
	if spine_bone in bones_a:
		bones_a[spine_bone].name = f"spine_{spine_counter}"
		for mesh in meshes:
			for vg in mesh.vertex_groups:
				if (vg.name == spine_bone):
					vg.name = f"spine_{spine_counter}"
		spine_counter += 1








# 3. Rename Input armature's bones and vertex groups to Human Citizen bones and vertex groups

for bone in arm_a.data.edit_bones:
	if (bone.name in bone_rename_table):
		bone.name = bone_rename_table[bone.name]

for mesh in meshes:
	for vg in mesh.vertex_groups:
		if (vg.name in bone_rename_table):
			vg.name = bone_rename_table[vg.name]







bpy.ops.object.mode_set(mode="EDIT")
for side in ["L","R"]:
	# 4. Create metacarpal bones in Input armature

	if not bones_a.get(f"finger_pinky_meta_{side}"):
		print(f"Creating finger_pinky_meta_{side}")

		parent = bones_a.get(f"hand_{side}")
		child = bones_a.get(f"finger_pinky_0_{side}")
		if child:
			createMidpointBone(bones_a, mesh, parent, child, f"finger_pinky_meta_{side}")


	if not bones_a.get(f"finger_ring_meta_{side}"):
		print(f"Creating finger_ring_meta_{side}")

		parent = bones_a.get(f"hand_{side}")
		child = bones_a.get(f"finger_ring_0_{side}")
		if child:
			createMidpointBone(bones_a, mesh, parent, child, f"finger_ring_meta_{side}")


	if not bones_a.get(f"finger_middle_meta_{side}"):
		print(f"Creating finger_middle_meta_{side}")

		parent = bones_a.get(f"hand_{side}")
		child = bones_a.get(f"finger_middle_0_{side}")
		if child:
			createMidpointBone(bones_a, mesh, parent, child, f"finger_middle_meta_{side}")


	if not bones_a.get(f"finger_index_meta_{side}"):
		print(f"Creating finger_index_meta_{side}")

		parent = bones_a.get(f"hand_{side}")
		child = bones_a.get(f"finger_index_0_{side}")
		if child:
			createMidpointBone(bones_a, mesh, parent, child, f"finger_index_meta_{side}")



	# 5. Create missing joint helper bones in Input armature

	if not bones_a.get(f"arm_elbow_helper_{side}"):
		parent = bones_a.get(f"arm_lower_{side}")
		if not parent:
			continue

		print(f"Creating joint helper bone: arm_elbow_helper_{side}")
		
		new_bone = bones_a.new(f"arm_elbow_helper_{side}")
		new_bone.head = parent.head.copy()
		new_bone.tail = parent.tail.copy()
		new_bone.parent = parent

	if not bones_a.get(f"leg_knee_helper_{side}"):
		parent = bones_a.get(f"leg_lower_{side}")
		if not parent:
			continue
		
		print(f"Creating joint helper bone: leg_knee_helper_{side}")

		new_bone = bones_a.new(f"leg_knee_helper_{side}")
		new_bone.head = parent.head.copy()
		new_bone.tail = parent.tail.copy()
		new_bone.parent = parent



	# 6. Create twist bones in arms and legs in Input armature

	# create upper twist bones in the appenditures
	if not bones_a.get(f"arm_upper_{side}_twist0"):
		print(f"Creating upper twist bone: arm_upper_{side}_twist0")

		new_bone = bones_a.new(f"arm_upper_{side}_twist0")
		bone = bones_a.get(f"arm_upper_{side}")
		new_bone.head = bone.head.copy()
		new_bone.tail = bone.tail.copy()
		new_bone.parent = bones_a[f"arm_upper_{side}"]

	if not bones_a.get(f"arm_lower_{side}_twist0"):
		print(f"Creating upper twist bone: arm_lower_{side}_twist0")

		new_bone = bones_a.new(f"arm_lower_{side}_twist0")
		bone = bones_a.get(f"arm_lower_{side}")
		new_bone.head = bone.head.copy()
		new_bone.tail = bone.tail.copy()
		new_bone.parent = bones_a[f"arm_lower_{side}"]

	if not bones_a.get(f"leg_upper_{side}_twist0"):
		print(f"Creating upper twist bone: leg_upper_{side}_twist0")

		new_bone = bones_a.new(f"leg_upper_{side}_twist0")
		bone = bones_a.get(f"leg_upper_{side}")
		new_bone.head = bone.head.copy()
		new_bone.tail = bone.tail.copy()
		new_bone.parent = bones_a[f"leg_upper_{side}"]

	if not bones_a.get(f"leg_lower_{side}_twist0"):
		print(f"Creating upper twist bone: leg_lower_{side}_twist0")

		new_bone = bones_a.new(f"leg_lower_{side}_twist0")
		bone = bones_a.get(f"leg_lower_{side}")
		new_bone.head = bone.head.copy()
		new_bone.tail = bone.tail.copy()
		new_bone.parent = bones_a[f"leg_lower_{side}"]


	# create lower twist bones in the midpoint of the appenditures
	if not bones_a.get(f"arm_upper_{side}_twist1"):
		print(f"Creating lower twist bone: arm_upper_{side}_twist1")

		parent = bones_a.get(f"arm_upper_{side}")
		child = bones_a.get(f"arm_lower_{side}")
		if child:
			createMidpointBone(bones_a, mesh, parent, child, f"arm_upper_{side}_twist1", False)

	if not bones_a.get(f"arm_lower_{side}_twist1"):
		print(f"Creating lower twist bone: arm_lower_{side}_twist1")

		parent = bones_a.get(f"arm_lower_{side}")
		child = bones_a.get(f"hand_{side}")
		if child:
			createMidpointBone(bones_a, mesh, parent, child, f"arm_lower_{side}_twist1", False)

	if not bones_a.get(f"leg_upper_{side}_twist1"):
		print(f"Creating lower twist bone: leg_upper_{side}_twist1")

		parent = bones_a.get(f"leg_upper_{side}")
		child = bones_a.get(f"leg_lower_{side}")
		if child:
			createMidpointBone(bones_a, mesh, parent, child, f"leg_upper_{side}_twist1", False)

	if not bones_a.get(f"leg_lower_{side}_twist1"):
		print(f"Creating lower twist bone: leg_lower_{side}_twist1")

		parent = bones_a.get(f"leg_lower_{side}")
		child = bones_a.get(f"ankle_{side}")
		if child:
			createMidpointBone(bones_a, mesh, parent, child, f"leg_lower_{side}_twist1", False)




# 7. Create any missing bones in the Human Citizen armature

bpy.ops.object.mode_set(mode="EDIT")
bones_a = arm_a.data.edit_bones
bones_b = arm_b.data.edit_bones

# create any missing bones that are in the Input armature but not the Human Citizen armature
for src_bone in bones_a:
	name = src_bone.name
	tgt_bone = bones_b.get(name)
	if not tgt_bone:
		print(f"Creating bone in {arm_b.name}: {name}")

		src_head_world = arm_a.matrix_world @ src_bone.head
		src_tail_world = arm_a.matrix_world @ src_bone.tail

		new_head = arm_b.matrix_world.inverted() @ src_head_world
		new_tail = arm_b.matrix_world.inverted() @ src_tail_world

		tgt_bone = bones_b.new(name)

		tgt_bone.head = new_head
		tgt_bone.tail = new_tail
		tgt_bone.roll = src_bone.roll

# set up the parents of the new bones
for src_bone in bones_a:
	tgt_bone = bones_b.get(src_bone.name)
	if src_bone.parent and src_bone.parent.name in bones_b:
		tgt_bone.parent = bones_b[src_bone.parent.name]



bones_a = arm_a.data.edit_bones
bones_b = arm_b.data.edit_bones

# remove any vestigial bones in the Human Citizen armature that don't exist in the Input armature
for src_bone in bones_b:
	tgt_bone = bones_a.get(src_bone.name)
	if not tgt_bone and src_bone.name not in protected_bones:
		print(f"Removing bone from {arm_b.name}: {src_bone.name}")
		arm_b.data.edit_bones.remove(src_bone)





# copy Human Citizen armature to create a reference armature
arm_c = arm_b.copy()
arm_c.data = arm_b.data.copy()
arm_c.name = "Reference Armature"
bpy.context.scene.collection.children.get("Output").objects.link(arm_c)




# 8. Move the Human Citizen armature's bones to match the Input armature in edit mode

arm_a.select_set(True)
arm_b.select_set(True)
bpy.ops.object.mode_set(mode="EDIT")
bones_a = arm_a.data.edit_bones
bones_b = arm_b.data.edit_bones

for name, src_bone in bones_a.items():
	tgt_bone = bones_b.get(name)
	if not tgt_bone:
		continue

	old_dir = (tgt_bone.tail - tgt_bone.head).normalized()
	old_len = tgt_bone.length

	src_head_world = arm_a.matrix_world @ src_bone.head
	new_head = arm_b.matrix_world.inverted() @ src_head_world

	tgt_bone.head = new_head
	tgt_bone.tail = tgt_bone.head + old_dir * old_len






# 9. Move the Input meshes to the Human Citizen armature

# set the meshes's armature to the Human Citizen skeleton
for mesh in meshes:
	for mod in mesh.modifiers:
		if mod.type == "ARMATURE":
			mod.object = bpy.data.objects.get(ARMATURE_B)
			break


	# pair the mesh to the Human Citizen skeleton
	bpy.ops.object.mode_set(mode="OBJECT")
	
	mesh_matrix = mesh.matrix_world.copy()
	mesh.parent = arm_b
	mesh.matrix_world = mesh_matrix

	coll_target = bpy.context.scene.collection.children.get("Output")
	coll_target.objects.link(mesh)
	coll_input = bpy.context.scene.collection.children.get("Input")
	coll_input.objects.unlink(mesh)







# 10. Weight paint the metacarpals in the Human Citizen armature

metacarpals = [
	"finger_pinky_meta_R",
	"finger_ring_meta_R",
	"finger_middle_meta_R",
	"finger_index_meta_R",
	"finger_pinky_meta_L",
	"finger_ring_meta_L",
	"finger_middle_meta_L",
	"finger_index_meta_L",
]

# automatically weight paint the metacarpals
bpy.ops.object.mode_set(mode="OBJECT")
bpy.ops.object.select_all(action="DESELECT")
arm_b.select_set(True)
for mesh in meshes:
	mesh.select_set(True)

bpy.ops.object.mode_set(mode="POSE")
bones_b = arm_b.pose.bones
for bone in metacarpals:
	if bone in bones_b:
		bones_b[bone].select = True

for mesh in meshes:
	bpy.context.view_layer.objects.active = mesh
	bpy.ops.object.mode_set(mode="WEIGHT_PAINT")
	bpy.ops.paint.weight_from_bones(type="AUTOMATIC")
	bpy.ops.object.vertex_group_limit_total(limit=4) # source 2 only allows 4 joint weights per vertex




# DEBUG: show all vertices with more than 4 vertex groups
#bpy.ops.object.mode_set(mode="OBJECT")
#for mesh in meshes:
#    bpy.context.view_layer.objects.active = mesh
#    mesh.select_set(True)
#
#bpy.ops.object.mode_set(mode="EDIT")
#bpy.ops.mesh.select_all(action="DESELECT")
#bpy.ops.object.mode_set(mode="OBJECT")
#
#for mesh in meshes:    
#    for v in mesh.data.vertices:
#        if len(v.groups) > 4:
#            v.select = True
#bpy.ops.object.mode_set(mode="EDIT")
#raise KeyboardInterrupt()





# 11. Pose the Human Citizen armature to match the reference armature shape

bone_point_table = {
	"neck_0": "head",

	#"spine_0": "spine_1", # sometimes people but multiple spine bones in the same spot so just don't try to align them, they're basically always pointing up anyway
	#"spine_1": "spine_2",
	#"spine_2": "",


	"clavicle_R": "arm_upper_R",
	"arm_upper_R": "arm_lower_R",
	"arm_upper_R_twist0": "arm_lower_R",
	"arm_upper_R_twist1": "arm_lower_R",
	
	"arm_lower_R_twist0": "hand_R",
	"arm_lower_R_twist1": "hand_R",
	"arm_lower_R": "hand_R",
	
	"hand_R": "",

	"finger_pinky_meta_R": "finger_pinky_0_R",
	"finger_pinky_0_R": "finger_pinky_1_R",
	"finger_pinky_1_R": "finger_pinky_2_R",
	"finger_pinky_2_R": "",
	
	"finger_ring_meta_R": "finger_ring_0_R",
	"finger_ring_0_R": "finger_ring_1_R",
	"finger_ring_1_R": "finger_ring_2_R",
	"finger_ring_2_R": "",

	"finger_middle_meta_R": "finger_middle_0_R",
	"finger_middle_0_R": "finger_middle_1_R",
	"finger_middle_1_R": "finger_middle_2_R",
	"finger_middle_2_R": "",

	"finger_index_meta_R": "finger_index_0_R",
	"finger_index_0_R": "finger_index_1_R",
	"finger_index_1_R": "finger_index_2_R",
	"finger_index_2_R": "",

	"finger_thumb_0_R": "finger_thumb_1_R",
	"finger_thumb_1_R": "finger_thumb_2_R",
	"finger_thumb_2_R": "",


	"clavicle_L": "arm_upper_L",
	"arm_upper_L": "arm_lower_L",
	"arm_upper_L_twist0": "arm_lower_L",
	"arm_upper_L_twist1": "arm_lower_L",
	
	"arm_lower_L_twist0": "hand_L",
	"arm_lower_L_twist1": "hand_L",
	"arm_lower_L": "hand_L",
	
	"hand_L": "",

	"finger_pinky_meta_L": "finger_pinky_0_L",
	"finger_pinky_0_L": "finger_pinky_1_L",
	"finger_pinky_1_L": "finger_pinky_2_L",
	"finger_pinky_2_L": "",
	
	"finger_ring_meta_L": "finger_ring_0_L",
	"finger_ring_0_L": "finger_ring_1_L",
	"finger_ring_1_L": "finger_ring_2_L",
	"finger_ring_2_L": "",

	"finger_middle_meta_L": "finger_middle_0_L",
	"finger_middle_0_L": "finger_middle_1_L",
	"finger_middle_1_L": "finger_middle_2_L",
	"finger_middle_2_L": "",

	"finger_index_meta_L": "finger_index_0_L",
	"finger_index_0_L": "finger_index_1_L",
	"finger_index_1_L": "finger_index_2_L",
	"finger_index_2_L": "",

	"finger_thumb_0_L": "finger_thumb_1_L",
	"finger_thumb_1_L": "finger_thumb_2_L",
	"finger_thumb_2_L": "",


	"leg_upper_R": "leg_lower_R",
	"leg_upper_R_twist0": "leg_lower_R",
	"leg_upper_R_twist1": "leg_lower_R",

	"leg_lower_R": "ankle_R",
	"leg_lower_R_twist0": "ankle_R",
	"leg_lower_R_twist1": "ankle_R",

	#"ankle_R": "ball_R",


	"leg_upper_L": "leg_lower_L",
	"leg_upper_L_twist0": "leg_lower_L",
	"leg_upper_L_twist1": "leg_lower_L",

	"leg_lower_L": "ankle_L",
	"leg_lower_L_twist0": "ankle_L",
	"leg_lower_L_twist1": "ankle_L",

	#"ankle_L": "ball_L",
}

bpy.context.view_layer.objects.active = arm_b
bpy.ops.object.mode_set(mode="EDIT")
bpy.ops.armature.select_all(action="DESELECT")
bones_b = arm_b.data.edit_bones

last_direction = 0
for bone in bone_point_table.keys():
	bpy.ops.object.mode_set(mode="EDIT")
	
	src_bone = bones_b.get(bone)
	if not src_bone:
		continue

	# point parent to a child so that the bones line up with the mesh
	if (bone_point_table[bone] != ""):
		tgt_bone = bones_b.get(bone_point_table[bone])
		if not tgt_bone:
			src_head = src_bone.head.copy()
			length = src_bone.length
			src_bone.tail = src_head + last_direction * length
			continue
	
		src_head = src_bone.head.copy()
		length = src_bone.length
		direction = (tgt_bone.head - src_head).normalized()
		src_bone.tail = src_head + direction * length

		last_direction = direction

	# if no target then just point in the same direction as parent
	if (bone_point_table[bone] == ""):
		src_head = src_bone.head.copy()
		length = src_bone.length
		src_bone.tail = src_head + last_direction * length





bpy.context.view_layer.objects.active = arm_c
bpy.ops.object.mode_set(mode="OBJECT")
bpy.ops.object.select_all(action="DESELECT")
arm_b.select_set(True)
arm_c.select_set(True)

# fix the rotations of the Human Citizen armature so that they match the reference armature
bpy.ops.object.mode_set(mode="EDIT")
for bone in bone_point_table.keys():
	if bone in arm_b.data.edit_bones:
		bpy.ops.armature.select_all(action="DESELECT")
	
		arm_b.data.edit_bones[bone].select = True
		arm_c.data.edit_bones[bone].select = True
		arm_c.data.edit_bones.active = arm_c.data.edit_bones[bone]

		bpy.ops.armature.calculate_roll(type="ACTIVE") # fix rotation bullshit where bones spin when their tail approaches 0 on an axis
bpy.ops.armature.select_all(action="DESELECT")




# pose the Human Citizen armature in the A-pose with no significant rotational differences to the reference armature, you want the difference between the Human Citizen armature and the reference armature to only be translational and scalable
bpy.ops.object.mode_set(mode="POSE")
for bone in bone_point_table.keys():
	if bone in arm_b.pose.bones:
		crr = arm_b.pose.bones[bone].constraints.new("COPY_ROTATION")
		crr.target = arm_c
		crr.name = "exp_rot"
		crr.subtarget = bone



# apply changes in pose mode to meshes
bpy.ops.object.mode_set(mode="OBJECT")
bpy.ops.object.select_all(action="DESELECT")
for mesh in meshes:
	bpy.context.view_layer.objects.active = mesh
	mesh.select_set(True)

	# merge armature with mesh permanently
	bpy.ops.object.modifier_apply(modifier="Armature")

	# create new armature on mesh
	mod = mesh.modifiers.new(name="Armature", type="ARMATURE")
	mod.object = arm_b


# set posed bones to be the rest pose
bpy.context.view_layer.objects.active = arm_b
bpy.ops.object.mode_set(mode="POSE")
bpy.ops.pose.armature_apply(selected=False)










# 12. Realign miscellaneous bones in the Human Citizen armature

ik_realign_table = {
	"hand_R_IK_target": "hand_R",
	"hand_R_to_L_ikrule": "hand_R",

	"hand_L_IK_target": "hand_L",
	"hand_L_to_R_ikrule": "hand_L",

	"foot_R_IK_target": "ankle_R",

	"foot_L_IK_target": "ankle_L",
}

bpy.context.view_layer.objects.active = arm_b
bpy.ops.object.mode_set(mode="EDIT")
bones_b = arm_b.data.edit_bones

# realign the IK bones
for src_bone_name in ik_realign_table.keys():
	bone_src = bones_b.get(src_bone_name)
	bone_target = bones_b.get(ik_realign_table[src_bone_name])
	bone_src_length = bone_src.length

	bone_src.head = bone_target.head.copy()
	bone_src.tail = bone_target.tail.copy()
	bone_src.roll = bone_target.roll
	bone_src.length = bone_src_length

# realign the hold bones
for side in ["L","R"]:
	bone_src = bones_b.get(f"hold_{side}")
	bone_src.roll = math.radians(-90)

	# try to reliably recreate the hold position regardless of model proportions
	bone_target = bones_b.get(f"finger_thumb_0_{side}")
	if bone_target:
		bone_src.head = bone_target.head.copy()
		bone_src.tail = bone_src.head
		bone_src.head.y -= 7
		bone_src.tail.y = bone_src.head.y - 10
		continue

	bone_target = bones_b.get(f"hand_{side}")
	if bone_target:
		bone_src.head = bone_target.head.copy() + (bone_target.tail - bone_target.head).normalized() * 2
		bone_src.tail = bone_src.head
		bone_src.head.y -= 10
		bone_src.tail.y = bone_src.head.y - 10
		continue

	bone_target = bones_b.get(f"ValveBiped.Anim_Attachment_{side}H")
	if bone_target:
		bone_src.head = bone_target.head.copy()
		bone_src.tail = bone_src.head
		bone_src.tail.y = bone_src.head.y - 10







if (not EYES_ENABLED):
	if "eye_L" in bones_b:
		bones_b["eye_L"].name = "eye_L_disabled"
	if "eye_R" in bones_b:
		bones_b["eye_R"].name = "eye_R_disabled"



# 13. Export the Human Citizen armature and meshes

bpy.ops.object.mode_set(mode="OBJECT")
bpy.ops.object.select_all(action="DESELECT")
arm_b.select_set(True)
for mesh in meshes:
	mesh.select_set(True)
	mesh.hide_viewport = False

export_path = os.path.join(bpy.path.abspath("//"),  os.path.splitext(os.path.basename(bpy.data.filepath))[0] + ".fbx")
bpy.ops.export_scene.fbx(
	filepath=export_path,
	use_selection=True,
	object_types={"ARMATURE", "MESH"},
	add_leaf_bones=False,
	primary_bone_axis="X",
	secondary_bone_axis="Z"
)



# 14. Cleanup

bpy.context.view_layer.layer_collection.children["Input"].exclude = True
bpy.ops.object.mode_set(mode="OBJECT")
bpy.ops.object.select_all(action="DESELECT")
bpy.context.view_layer.objects.active = arm_c
arm_c.select_set(True)
bpy.ops.object.delete()

# remove all copy rotation constraints
bpy.context.view_layer.objects.active = arm_b
for pbone in arm_b.pose.bones:
	if pbone.constraints:
		for c in pbone.constraints:
			pbone.constraints.remove(c)


# remove all unused vertex groups
bones_b_names = [b.name for b in arm_b.data.bones]
for mesh in meshes:
	for vg in [vg for vg in mesh.vertex_groups if vg.name not in bones_b_names]:
		print(f"Removing unused vertex group in {mesh.name}: {vg.name}")
		mesh.vertex_groups.remove(vg)
