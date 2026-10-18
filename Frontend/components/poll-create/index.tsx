"use client"

import { useState, useCallback, useEffect } from "react"
import { motion, AnimatePresence } from "framer-motion"
import { ArrowLeft, ArrowRight, Rocket, Loader2, X, Sparkles } from "lucide-react"
import { useRouter, useSearchParams } from "next/navigation"
import { Button } from "@/components/ui/button"
import { StepIndicator } from "./step-indicator"
import { QuestionStep } from "./steps/question-step"
import { DescriptionStep } from "./steps/description-step"
import { MediaStep } from "./steps/media-step"
import { LinkStep } from "./steps/link-step"
import { PrivacyStep } from "./steps/privacy-step"
import { PreviewStep } from "./steps/preview-step"
import { getInitialDraft, STEPS, type PollDraft } from "@/lib/poll-creation"
import { pollsApi, type ApiPoll } from "@/lib/api"
import { DEFAULT_CATEGORY } from "@/lib/categories"
import { cn } from "@/lib/utils"

export function PollCreator() {
  const router = useRouter()
  const remixParam = useSearchParams().get("remix")
  const remixRequested = remixParam !== null
  const remixId = remixParam && /^\d+$/.test(remixParam) ? Number(remixParam) : 0
  const invalidRemixId = remixRequested && (!Number.isSafeInteger(remixId) || remixId <= 0)
  const [currentStep, setCurrentStep] = useState(1)
  const [draft, setDraft] = useState<PollDraft>(getInitialDraft())
  const [loadedRemix, setLoadedRemix] = useState<{ id: number; poll: ApiPoll } | null>(null)
  const [loadRemixError, setLoadRemixError] = useState<{ id: number; message: string } | null>(null)
  const remixSource = loadedRemix?.id === remixId ? loadedRemix.poll : null
  const remixError = invalidRemixId ? "This original poll link is invalid." : loadRemixError?.id === remixId ? loadRemixError.message : null
  const [errors, setErrors] = useState<Record<string, string>>({})
  const [isPublishing, setIsPublishing] = useState(false)
  const [isPublished, setIsPublished] = useState(false)
  const [completedSteps, setCompletedSteps] = useState<Set<number>>(new Set())

  useEffect(() => {
    if (!remixRequested || invalidRemixId) return
    let active = true
    pollsApi.getById(remixId).then((source) => {
      if (source.isPrivate || source.isWellness || source.moderationStatus !== "Published")
        throw new Error("This poll is not available to remix.")
      if (active) setLoadedRemix({ id: remixId, poll: source })
    }).catch((error) => {
      if (active) setLoadRemixError({ id: remixId, message: error instanceof Error ? error.message : "Could not load the original poll." })
    })
    return () => { active = false }
  }, [remixRequested, invalidRemixId, remixId])

  const updateDraft = useCallback(<K extends keyof PollDraft>(key: K, value: PollDraft[K]) => {
    setDraft((prev) => ({ ...prev, [key]: value }))
    setErrors((prev) => ({ ...prev, [key]: "" }))
  }, [])

  const validateStep = useCallback((step: number): boolean => {
    if (step === 1 && (!draft.question.trim() || remixSource && draft.question.trim().toLowerCase() === remixSource.question.trim().toLowerCase())) {
      setErrors({ question: draft.question.trim() ? "Write a different question for your remix" : "Please enter a question" })
      return false
    }
    return true
  }, [draft.question, remixSource])

  const goToStep = useCallback((step: number) => {
    if (step < 1 || step > STEPS.length) return
    if (step > currentStep && !validateStep(currentStep)) return
    
    // Mark current step as completed when moving forward
    if (step > currentStep) {
      setCompletedSteps((prev) => new Set([...prev, currentStep]))
    }
    
    setCurrentStep(step)
  }, [currentStep, validateStep])

  const nextStep = () => goToStep(currentStep + 1)
  const prevStep = () => goToStep(currentStep - 1)
  const skipStep = () => {
    setCompletedSteps((prev) => new Set([...prev, currentStep]))
    goToStep(currentStep + 1)
  }

  const handlePublish = async () => {
    if (remixRequested && !remixSource) {
      setErrors({ question: remixError || "Waiting for the original poll to load." })
      return
    }
    if (remixSource && draft.question.trim().toLowerCase() === remixSource.question.trim().toLowerCase()) {
      setCurrentStep(1)
      setErrors({ question: "Write a different question for your remix" })
      return
    }
    if (!validateStep(currentStep)) return

    setIsPublishing(true)
    try {
      // Expires 7 days from now by default
      const expiresAt = new Date(Date.now() + 7 * 24 * 60 * 60 * 1000).toISOString()

      const media = draft.media ? await pollsApi.uploadMedia(draft.media.file) : null
      await pollsApi.create({
        question:    draft.question.trim(),
        description: draft.description.trim(),
        category:    remixSource?.category ?? DEFAULT_CATEGORY,
        expiresAt,
        options:     ["Support", "Against"],
        creativeTemplate: draft.creativeTemplate,
        reactionId: draft.reactionId ?? undefined,
        resultVisibility: draft.resultVisibility,
        remixedFromPollId: remixSource?.id,
        sourceType:  draft.externalLink ? `manual-${draft.externalLink.platform}` : "manual",
        sourceUrl:   draft.externalLink?.url ?? undefined,
        thumbnailUrl: media?.url,
        mediaType: media?.mediaType,
        isAIGenerated: false,
      })

      setIsPublished(true)
      setTimeout(() => router.push("/"), 2000)
    } catch (err) {
      console.error("Publish failed:", err)
      setErrors({ question: err instanceof Error ? (err.message || `Publish failed (HTTP ${"status" in err ? err.status : "unknown"}). Please try again later.`) : "Failed to publish poll. Please try again." })
    } finally {
      setIsPublishing(false)
    }
  }

  const isLastStep = currentStep === STEPS.length
  const canGoNext = currentStep === 1 ? draft.question.trim().length > 0 : true

  // Submitted success state
  if (isPublished) {
    return (
      <div className="flex min-h-[80vh] flex-col items-center justify-center px-4">
        <motion.div
          initial={{ scale: 0 }}
          animate={{ scale: 1 }}
          transition={{ type: "spring", stiffness: 200, damping: 15 }}
          className="mb-6 rounded-full bg-emerald-500/10 p-8"
        >
          <motion.div
            initial={{ scale: 0, rotate: -180 }}
            animate={{ scale: 1, rotate: 0 }}
            transition={{ delay: 0.2, type: "spring" }}
          >
            <Rocket className="h-16 w-16 text-emerald-500" />
          </motion.div>
        </motion.div>
        <motion.h2
          initial={{ opacity: 0, y: 20 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ delay: 0.3 }}
          className="text-2xl font-bold text-foreground"
        >
          Poll Submitted!
        </motion.h2>
        <motion.p
          initial={{ opacity: 0, y: 20 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ delay: 0.4 }}
          className="mt-2 text-center text-muted-foreground"
        >
          Your poll is in review. Redirecting to home...
        </motion.p>
        
        {/* Confetti-like particles */}
        {[...Array(12)].map((_, i) => (
          <motion.div
            key={i}
            className="absolute h-3 w-3 rounded-full"
            style={{
              backgroundColor: ["#f472b6", "#34d399", "#fbbf24", "#60a5fa", "#a78bfa"][i % 5],
            }}
            initial={{
              opacity: 1,
              x: 0,
              y: 0,
              scale: 0,
            }}
            animate={{
              opacity: 0,
              x: ((i * 73) % 300) - 150,
              y: ((i * 131) % 300) - 150,
              scale: [0, 1, 0.5],
            }}
            transition={{
              duration: 1.5,
              delay: 0.2 + i * 0.05,
              ease: "easeOut",
            }}
          />
        ))}
      </div>
    )
  }

  return (
    <div className="flex min-h-dvh min-w-0 flex-col bg-background">
      {/* Header */}
      <header className="sticky top-0 z-50 border-b border-border/50 bg-background/90 glass">
        <div className="mx-auto flex h-16 max-w-3xl items-center justify-between px-4">
          <Button
            variant="ghost"
            size="sm"
            onClick={() => router.back()}
            className="text-muted-foreground hover:text-foreground"
          >
            <X className="h-5 w-5" />
          </Button>
          
          <div className="flex items-center gap-2">
            <Sparkles className="h-5 w-5 text-primary" />
            <span className="font-semibold text-foreground">{remixRequested ? "Remix Poll" : "Create Poll"}</span>
          </div>

          <div className="w-10" /> {/* Spacer for centering */}
        </div>
      </header>
      {remixRequested && <div className="border-b bg-secondary/40 px-4 py-3 text-center text-sm">
        {remixError ? <span role="alert" className="text-destructive">{remixError}</span> : remixSource ? <><span className="font-semibold">Inspired by {remixSource.createdByDisplayName || remixSource.createdByUsername || "a PoP poll"}</span><span className="block text-muted-foreground">{remixSource.question}</span><span className="block text-muted-foreground">Write your own question. Media and votes are not copied.</span></> : "Loading original poll…"}
      </div>}

      {/* Step Indicator */}
      <div className="border-b border-border/50 bg-card/50 px-4 py-4">
        <div className="mx-auto max-w-3xl">
          <StepIndicator
            currentStep={currentStep}
            onStepClick={goToStep}
            completedSteps={completedSteps}
          />
        </div>
      </div>

      {/* Content */}
      <main className="flex-1 overflow-y-auto">
        <div className="mx-auto max-w-xl px-4 py-5 sm:py-8">
          <AnimatePresence mode="wait">
            {currentStep === 1 && (
              <QuestionStep
                key="question"
                value={draft.question}
                onChange={(v) => updateDraft("question", v)}
                error={errors.question}
                creativeTemplate={draft.creativeTemplate}
                reactionId={draft.reactionId}
                onTemplateChange={(v) => updateDraft("creativeTemplate", v)}
                onReactionChange={(v) => updateDraft("reactionId", v)}
              />
            )}
            {currentStep === 2 && (
              <DescriptionStep
                key="description"
                value={draft.description}
                onChange={(v) => updateDraft("description", v)}
                onSkip={skipStep}
              />
            )}
            {currentStep === 3 && (
              <MediaStep
                key="media"
                value={draft.media}
                onChange={(v) => updateDraft("media", v)}
                onSkip={skipStep}
              />
            )}
            {currentStep === 4 && (
              <LinkStep
                key="link"
                value={draft.externalLink}
                onChange={(v) => updateDraft("externalLink", v)}
                onSkip={skipStep}
              />
            )}
            {currentStep === 5 && (
              <PrivacyStep
                key="privacy"
                value={draft.privacy}
                onChange={(v) => updateDraft("privacy", v)}
                resultVisibility={draft.resultVisibility}
                onResultVisibilityChange={(v) => updateDraft("resultVisibility", v)}
              />
            )}
            {currentStep === 6 && (
              <PreviewStep key="preview" draft={draft} remixSource={remixSource} />
            )}
          </AnimatePresence>
        </div>
      </main>

      {/* Footer Navigation */}
      <footer className="sticky bottom-0 border-t border-border/50 bg-background/90 glass safe-bottom">
        <div className="mx-auto flex min-h-20 max-w-xl flex-wrap items-center justify-between gap-2 px-4 py-3 sm:flex-nowrap sm:gap-4">
          {/* Back Button */}
          <Button
            variant="outline"
            onClick={prevStep}
            disabled={currentStep === 1}
            className={cn(
              "min-h-11 rounded-xl px-4 sm:px-6",
              currentStep === 1 && "invisible"
            )}
          >
            <ArrowLeft className="mr-2 h-4 w-4" />
            Back
          </Button>

          {/* Step Info */}
          <div className="text-center">
            <p className="text-sm font-medium text-foreground">
              {STEPS[currentStep - 1].title}
            </p>
            <p className="text-xs text-muted-foreground">
              Step {currentStep} of {STEPS.length}
            </p>
          </div>

          {isLastStep && errors.question && <p role="alert" className="text-sm text-destructive">{errors.question}</p>}
          {/* Next/Publish Button */}
          {isLastStep ? (
            <Button
              onClick={handlePublish}
              disabled={isPublishing || !draft.question.trim() || (remixRequested && !remixSource)}
              className="rounded-xl bg-gradient-to-r from-primary to-primary/80 px-6 font-semibold shadow-lg shadow-primary/25"
            >
              {isPublishing ? (
                <>
                  <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                  Publishing...
                </>
              ) : (
                <>
                  <Rocket className="mr-2 h-4 w-4" />
                  Publish
                </>
              )}
            </Button>
          ) : (
            <Button
              onClick={nextStep}
              disabled={!canGoNext}
              className="rounded-xl px-6"
            >
              Next
              <ArrowRight className="ml-2 h-4 w-4" />
            </Button>
          )}
        </div>
      </footer>
    </div>
  )
}
