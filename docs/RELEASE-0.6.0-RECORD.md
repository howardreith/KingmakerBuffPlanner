# 0.6.0 release record

Preparation checkpoint: owner explicitly authorizes merging PR10/controller work
into the default branch, pushing it and publishing a full release with a new
version. Selected0.6.0 / CLR0.6.0.0; prior tags/assets remain immutable.

Disposition: OwnerAuthorizedPreRuntime, using the established unchanged guarded
publisher, not ConfirmHumanAcceptance. Native F01-F10 remain NOT_RUN; publication
does not complete the controller mission. No gameplay-mod/installation/save work.

Preserve the active Alpha4 safety process26124/exec83416 and frozen69a48a4; its
eventual outcome is separate. No repeated full-history inventory is selected by
this release route. Pre-merge validation and final product/DLL/ZIP/MVID identities
are recorded after their actual completion, not inferred from an earlier alpha.

Default branch main has a legitimate existing local worktree. Release integration
therefore uses an isolated clone under private/release, without resetting or
checking out that occupied worktree. Feature and main pushes use the project-owned
guarded helper; local non-fast-forward merge preserves feature history. The final
publisher requires clean main exactly matching origin/main and an unused tag.
