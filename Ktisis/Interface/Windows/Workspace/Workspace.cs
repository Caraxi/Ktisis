using System.Numerics;

using ImGuiNET;

using Dalamud.Interface;

using Ktisis.Util;
using Ktisis.Overlay;
using Ktisis.Localization;
using Ktisis.Structs.Actor;
using Ktisis.Structs.Bones;
using Ktisis.Interface.Components;
using Ktisis.Interface.Windows.ActorEdit;

namespace Ktisis.Interface.Windows.Workspace {
	public static class Workspace {
		public static bool Visible = false;

		public static Vector4 ColGreen = new Vector4(0, 255, 0, 255);
		public static Vector4 ColRed = new Vector4(255, 0, 0, 255);

		public static TransformTable Transform = new();

		// Toggle visibility

		public static void Show() {
			Visible = true;
		}

		public static void Hide() {
			Visible = false;
		}

		// Draw window

		public static void Draw() {
			if (!Visible)
				return;

			var gposeOn = Ktisis.IsInGPose;

			var size = new Vector2(-1, -1);
			ImGui.SetNextWindowSize(size, ImGuiCond.Always);
			ImGui.SetNextWindowSizeConstraints(size, size);

			ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(10, 10));

			if (ImGui.Begin("Ktisis (Alpha)", ref Visible, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.AlwaysAutoResize)) {
				ImGui.BeginGroup();
				ImGui.AlignTextToFramePadding();

				ImGui.TextColored(
					gposeOn ? ColGreen : ColRed,
					gposeOn ? "GPose Enabled" : "GPose Disabled"
				);


				ImGui.SameLine();

				// Pose switch
				ControlButtons.DrawPoseSwitch();

				// control buttons (gizmo op + extra)
				ControlButtons.Draw();

				// Actor control
				ActorControl();
			}

			ImGui.PopStyleVar(1);
			ImGui.End();
		}

		// Actor control

		private unsafe static void ActorControl() {
			var cfg = Ktisis.Configuration;

			// Get target actor, model, etc

			var target = Ktisis.GPoseTarget;
			if (target == null) return;

			var actor = (Actor*)Ktisis.GPoseTarget!.Address;
			if (actor->Model == null) return;

			// Draw co-ordinate table
			TransformTableAndExtra(actor);

			// Animation control
			AnimationControls.Draw(target);

			// Gaze control
			if (ImGui.CollapsingHeader("Gaze Control"))
				EditGaze.Draw(actor);

			// Bone categories
			if (ImGui.CollapsingHeader("Bone Category Visibility")) {

				ImGui.Indent(16.0f);
				ImGui.Columns(2);
				int i = 0;
				bool hasShownAnyCategory = false;
				foreach (Category category in Category.Categories.Values) {
					if (!category.ShouldDisplay) continue;

					bool categoryState = cfg.IsBoneCategoryVisible(category);

					if (ImGui.Checkbox(category.Name, ref categoryState))
						cfg.ShowBoneByCategory[category.Name] = categoryState;

					if (i % 2 != 0) ImGui.NextColumn();
					i++;
					hasShownAnyCategory = true;
				}
				ImGui.Columns();
				if (!hasShownAnyCategory) {
					ImGui.Text("No bone found.");
					ImGui.Text("Show Skeleton (");
					ImGui.SameLine();
					GuiHelpers.Icon(FontAwesomeIcon.EyeSlash);
					ImGui.SameLine();
					ImGui.Text(") to fill this.");
				}

				ImGui.Unindent(16.0f);
			}

			// Bone tree
			BoneTree.Draw(actor);
		}

		// Transform Table actor and bone names display, actor related extra

		private static unsafe bool TransformTableAndExtra(Actor* target) {
			float panelHeight = ImGui.GetTextLineHeight() * 2 + ImGui.GetStyle().ItemSpacing.Y + ImGui.GetStyle().FramePadding.Y; // + ImGui.GetStyle().FramePadding.Y

			// Customize button
			if (GuiHelpers.IconButtonTooltip(FontAwesomeIcon.UserEdit, "Edit targeted Actor's appearance.", new Vector2(ControlButtons.ButtonSize.X, panelHeight)))
				if (EditActor.Visible) EditActor.Hide();
				else EditActor.Show();
			ImGui.SameLine();

			var select = Skeleton.BoneSelect;
			var bone = Skeleton.GetSelectedBone(target->Model->Skeleton);

			var frameSize = new Vector2(ImGui.GetContentRegionAvail().X, panelHeight);
			ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(ImGui.GetStyle().FramePadding.X, ImGui.GetStyle().FramePadding.Y / 2));
			if (ImGui.BeginChildFrame(8, frameSize, ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoScrollbar)) {

				// display target name
				string targetName = target->GetNameOr($"{Locale.GetString("Target")} #" + target->ObjectID);
				ImGui.Text($"{targetName}");

				// display selected bone name
				if (select.Active && bone != null) {
					ImGui.SetCursorPosY(ImGui.GetCursorPosY() - (ImGui.GetStyle().ItemSpacing.Y / 2) - (ImGui.GetStyle().FramePadding.Y / 2));
					ImGui.Text($"{bone.LocaleName}");
				}

				ImGui.EndChildFrame();
			}
			ImGui.PopStyleVar();

			// Draw Transform Table
			if (!select.Active) return Transform.Draw(target);
			if (bone == null) return false;
			return Transform.Draw(bone);
		}
	}
}
