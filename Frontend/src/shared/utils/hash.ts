export const getColorFromId = (id: string): string => {
    if (!id) return "808080";
    let hash = 0;
    for (let i = 0; i < id.length; i++) {
        hash = id.charCodeAt(i) + ((hash << 5) - hash);
    }
    const color = Math.abs(hash).toString(16).substring(0, 6).padStart(6, "0");
    return color;
};
