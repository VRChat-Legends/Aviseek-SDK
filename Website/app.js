// filled in by the listing build
const LISTING_URL = "{{ listingInfo.Url }}";
const VCC_URL = "vcc://vpm/addRepo?url=" + encodeURIComponent(LISTING_URL);

document.querySelectorAll("#vccButton, .vccLink").forEach((link) => {
  link.href = VCC_URL;
});

const copyButton = document.getElementById("copyListing");
copyButton.addEventListener("click", async () => {
  const label = copyButton.querySelector("span");
  try {
    await navigator.clipboard.writeText(LISTING_URL);
    label.textContent = "Copied";
  } catch {
    const field = document.getElementById("listingUrl");
    field.select();
    label.textContent = "Press Ctrl+C";
  }
  setTimeout(() => (label.textContent = "Copy"), 2000);
});
