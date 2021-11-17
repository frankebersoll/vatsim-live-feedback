module Client.Constants

let githubProjectUrl =
    "https://github.com/frankebersoll/vatsim-live-feedback"

let discordInviteUrl =
    "https://discord.gg/"
    + (Lib.Parcel.envVariable "VLF_DISCORD_INVITATION_CODE")
