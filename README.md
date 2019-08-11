# Gifski Win

A native Windows GUI making use of [`ffmpeg`](https://ffmpeg.org/)
([source](https://github.com/FFmpeg/FFmpeg)) and [`gifski`](https://gif.ski/)
([source](https://github.com/ImageOptim/gifski)).

This project was inspired by [this tweet](https://twitter.com/i/status/1158893237320523776)
advertising the [gifski GUI for Mac](https://github.com/sindresorhus/Gifski). I
thought, "hey, I could make a Windows version of that!"

And so I did!

## Screenshots

![Screenshot of the drag'n'drop functionality.](https://i.imgur.com/J4IOGUx.png)

<sup>Rainbow background thing courtesy of the gifski Mac GUI project.</sup>

![Screenshot of the video editor (still unimplemented).](https://i.imgur.com/CsZ37fB.png)

<sup>Still unimplemented video editing tools.</sup>

![Screenshot of the progress bar.](https://i.imgur.com/bxIHVcu.png)

![Screenshot of the progress being shown in the task bar](https://i.imgur.com/0pY8HU4.png)

<sup>I'll potentially look for a circular progress thing, just didn't bother at
this early stage.</sup>

![Screenshot of the final screen, allowing the user to copy or save the image.](https://i.imgur.com/edahGVY.png)

<sup>Video courtesy of [Big Buck Bunny](https://www.bigbuckbunny.org/).</sup>

## TODOs

- [ ] Fix crash (from `.SetAnimatedSource`) when generated GIF is too large
- [ ] Check if dragged file is actually a video file / can be converted
- [ ] Delete previous output that is being left on `%temp%`
- [ ] Add cancel functionality to conversion window
- [ ] Cropping and other tweaking settings to the video pre-conversion
- [ ] The GIF conversion is being slow, for some reason. This might be a
misconfiguration issue, or perhaps it might just be how the Windows port of
gifski itself runs, in which case that's sad :T
- [ ] Properly styling the buttons, and improving the overall design of the
windows

## Notes

GIFs are archaic and not very efficient. It is more than likely that the original
video file will be much smaller in size compared to the generated GIF. Keep that
in mind when considering using GIFs.

Also, it's pronounced /ɡɪf/.