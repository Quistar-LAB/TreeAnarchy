#pragma once
#ifndef __TREEANARCHY_H__
#define __TREEANARCHY_H__

using namespace System;
using namespace ICities;

namespace TreeAnarchyNative {
	public ref class TreeAnarchy : IUserMod
	{
	internal:
		literal String^ m_modVersion = "0.0.1";
	private:
		literal String^ m_modName = "Tree Anarchy";
		literal String^ m_modDesc = "A reboot of Unlimited Trees, let's you plant more trees with snapping";
	public:
		virtual property System::String^ Name {
			String^ get() { return m_modName + " " + m_modVersion; }
		}
		virtual property System::String^ Description {
			String^ get() { return m_modDesc; }
		}
		void OnEnabled();
		void OnDisabled();
		void OnSettingsUI(UIHelperBase^);
	};
}

#endif