import { create } from "zustand";

type HomeTabType = "listings" | "campaigns";
type ProfileTabType = "channels" | "campaigns";

interface ListState {
    homeActiveTab: HomeTabType;
    profileActiveTab: ProfileTabType;
    setHomeActiveTab: (tab: HomeTabType) => void;
    setProfileActiveTab: (tab: ProfileTabType) => void;
}

export const useListStateStore = create<ListState>((set) => ({
    homeActiveTab: "listings",
    profileActiveTab: "channels",
    setHomeActiveTab: (tab) => set({ homeActiveTab: tab }),
    setProfileActiveTab: (tab) => set({ profileActiveTab: tab }),
}));
