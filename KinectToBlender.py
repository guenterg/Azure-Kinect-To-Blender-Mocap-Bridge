bl_info = {
    "name": "Motion from Azure Kinect",
    "blender": (2, 80, 0),
    "category": "Object",
}
import bpy
from bpy.types import Armature, Pose, PoseBone

class ReadLocationsFromFile(bpy.types.Operator):
    """Capture Motion From Mocap Text File."""      # Use this as a tooltip for menu items and buttons.
    bl_idname = "armature.position_bones"        # Unique identifier for buttons and menu items to reference.
    bl_label = "Reposition Armature Bones"         # Display name in the interface.
    bl_options = {'REGISTER', 'UNDO'}  # Enable undo for the operator.

    def execute(self, context:bpy.context):        # execute() is called when running the operator.
        scene = context.scene
        armature:Armature = scene.objects['armature']
        armature.select_set(True)
        bpy.ops.object.mode_set(mode = 'POSE')
        hbone : PoseBone = armature.pose.bones['hand.R']
        hbone.rotation_mode = 'XYZ'
        hbone.rotation_euler = [50,30,60]
#        for obj in scene.objects:
#            obj.location.x += 1.0
        return {'FINISHED'}            # Lets Blender know the operator finished successfully.

def menu_func(self, context:bpy.context):
    self.layout.operator(ReadLocationsFromFile.bl_idname)

    

def register():
    bpy.utils.register_class(ReadLocationsFromFile)
    bpy.types.VIEW3D_MT_object.append(menu_func)  # Adds the new operator to an existing menu.

def unregister():
    bpy.utils.unregister_class(ReadLocationsFromFile)



if __name__ == "__main__":
    register()

    #todo: adjust mocap data to scale of armature
    #todo: adjust armature to scale of mocap data